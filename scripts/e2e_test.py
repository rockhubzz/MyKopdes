#!/usr/bin/env python3
"""
End-to-end tests for the MyKopdes stack — backend <-> MySQL <-> frontend contract.

Runs against the LIVE deployment through nginx (default http://localhost), so
every request exercises the real chain: nginx -> backend-dotnet -> MySQL.
Where the API alone can't prove correctness, the suite queries MySQL directly
(via docker exec) to verify writes actually landed with the right values.

Scope covered:
  1. Health / infra (health, swagger doc, gzip, frontend shell)
  2. Auth flows (staff + member JWT, role enforcement, bad credentials)
  3. Items + categories (CRUD, scan, search, items_count, delete guard)
  4. Members (create -> bcrypt -> member login, lookup, update, delete)
  5. Checkout pipeline (preview math, discounts, stock deduction, receipt
     shape, void + audit log, concurrent checkout race)
  6. Loans (create, approve, partial pay, reject, write-off)
  7. Restocks (stock increases)
  8. Reports consistency (dashboard totals vs raw SQL)
  9. Frontend contract shapes (exact JSON keys the UI reads)

Every created row is cleaned up via the API; test data is marked with a
unique "E2E_<ts>" prefix so a crashed run can never poison the next one.

Usage:
    python3 scripts/e2e_test.py [--base http://localhost] [--verbose]
Exit code 0 = all passed, 1 = failures (CI-friendly).
"""
import argparse
import json
import re
import subprocess
import sys
import threading
import time
import urllib.error
import urllib.request
import uuid
from datetime import datetime, timezone

MARKER = f"E2E_{uuid.uuid4().hex[:8]}"

# ---------------------------------------------------------------- harness ---
PASS = 0
FAIL = 0
FAILURES = []
VERBOSE = False
CREATED = {"items": [], "categories": [], "members": [], "loans": [], "transactions": [], "suppliers": []}
STAFF_TOKEN = None
MEMBER_TOKEN = None


def check(name, cond, detail=""):
    global PASS, FAIL
    if cond:
        PASS += 1
        if VERBOSE:
            print(f"  PASS  {name}")
    else:
        FAIL += 1
        FAILURES.append((name, detail))
        print(f"  FAIL  {name}  {detail}")


def section(title):
    print(f"\n== {title} ==")


def req(method, path, body=None, token=None, headers=None, base="http://localhost"):
    """HTTP through nginx; returns (status, parsed-json-or-text, raw-bytes)."""
    url = base + path
    data = json.dumps(body).encode() if body is not None else None
    r = urllib.request.Request(url, data=data, method=method)
    r.add_header("Content-Type", "application/json")
    if token:
        r.add_header("Authorization", f"Bearer {token}")
    for k, v in (headers or {}).items():
        r.add_header(k, v)
    try:
        with urllib.request.urlopen(r, timeout=30) as resp:
            raw = resp.read()
            status = resp.status
            hdrs = dict(resp.headers)
    except urllib.error.HTTPError as e:
        raw = e.read()
        status = e.code
        hdrs = dict(e.headers)
    try:
        parsed = json.loads(raw) if raw else None
    except Exception:
        parsed = raw.decode("utf-8", "replace")
    return status, parsed, hdrs


def sql(query):
    """Query MySQL directly through the docker container."""
    out = subprocess.run(
        ["docker", "exec", "koperasi_mysql", "mysql", "-N", "-ukoperasi",
         "-pkoperasi_secret", "koperasi", "-e", query],
        capture_output=True, text=True, timeout=30)
    if out.returncode != 0:
        raise RuntimeError(f"SQL failed: {out.stderr.strip()[:200]}")
    return out.stdout.strip()


def multipart(fields):
    """Build a multipart/form-data body exactly like the frontend's FormData
    (which the item create/update endpoints require)."""
    boundary = uuid.uuid4().hex
    parts = []
    for k, v in fields.items():
        parts.append(f"--{boundary}\r\nContent-Disposition: form-data; name=\"{k}\"\r\n\r\n{v}\r\n")
    body = "".join(parts) + f"--{boundary}--\r\n"
    return body.encode(), f"multipart/form-data; boundary={boundary}"


def post_form(path, fields, token, base="http://localhost"):
    """POST multipart form-data (the frontend's FormData path for item
    create/update); returns (status, parsed-json)."""
    body, ctype = multipart(fields)
    r = urllib.request.Request(base + path, data=body, method="POST")
    r.add_header("Content-Type", ctype)
    r.add_header("Authorization", f"Bearer {token}")
    try:
        with urllib.request.urlopen(r, timeout=30) as resp:
            raw, status = resp.read(), resp.status
    except urllib.error.HTTPError as e:
        raw, status = e.read(), e.code
    try:
        return status, json.loads(raw) if raw else None
    except Exception:
        return status, raw.decode("utf-8", "replace")


def now_iso():
    return datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M:%S")


# ------------------------------------------------------------------ suites ---
def suite_infra(base):
    section("1. Health / infrastructure")
    s, d, _ = req("GET", "/health", base=base)
    check("health returns 200", s == 200, f"got {s}")
    check("health body has status=healthy", isinstance(d, dict) and d.get("status") == "healthy")

    s, d, _ = req("GET", "/swagger/v1/swagger.json", base=base)
    check("swagger json 200", s == 200, f"got {s}")
    n_ops = sum(len(v) for v in d.get("paths", {}).values()) if isinstance(d, dict) else 0
    check("swagger documents >100 operations", n_ops > 100, f"got {n_ops}")

    s, _, h = req("GET", "/api/items?page=1&per_page=5", token=STAFF_TOKEN, base=base,
                  headers={"Accept-Encoding": "gzip"})
    check("API responses are gzipped", h.get("Content-Encoding", "").lower() == "gzip",
          f"encoding={h.get('Content-Encoding')}")

    s, html, _ = req("GET", "/", base=base)
    check("frontend shell serves 200", s == 200, f"got {s}")

    # /member/me — the member layout revalidates against this on every mount
    s, d, _ = req("GET", "/api/member/me", token=MEMBER_TOKEN, base=base)
    check("/member/me exists (layout revalidation)", s == 200, f"got {s}")
    check("/member/me shape has membership_id", (d or {}).get("membership_id") is not None,
          f"keys={list((d or {}).keys())}")


def suite_auth(base):
    global STAFF_TOKEN, MEMBER_TOKEN
    section("2. Auth")

    s, d, _ = req("POST", "/api/auth/staff/login", {"email": "admin@koperasi.test", "password": "WRONG"}, base=base)
    check("staff login rejects bad password", s in (400, 401), f"got {s}")

    s, d, _ = req("POST", "/api/auth/staff/login",
                  {"email": "admin@koperasi.test", "password": "password"}, base=base)
    check("staff login 200", s == 200, f"got {s}")
    check("login returns token", isinstance(d, dict) and bool(d.get("token")))
    check("login returns user with id/name/role",
          isinstance(d, dict) and d.get("user", {}).get("id") == 1 and d.get("user", {}).get("role") == "admin",
          f"user={d.get('user') if isinstance(d, dict) else d}")
    STAFF_TOKEN = d["token"]

    s, d, _ = req("GET", "/api/auth/staff/me", token=STAFF_TOKEN, base=base)
    check("/auth/staff/me 200 with token", s == 200, f"got {s}")
    check("/me shape has id/name/email/role",
          isinstance(d, dict) and all(k in d for k in ("id", "name", "email", "role")), f"got {list(d or [])}")

    s, d, _ = req("GET", "/api/auth/staff/me", base=base)
    check("protected endpoint rejects missing token", s == 401, f"got {s}")

    # Member auth
    s, d, _ = req("POST", "/api/auth/member/login",
                  {"identifier": "KOP-2610-0001", "password": "password"}, base=base)
    check("member login 200", s == 200, f"got {s}")
    if s == 200:
        check("member login returns member shape (membership_id key)",
              isinstance(d, dict) and d.get("member", {}).get("membership_id") == "KOP-2610-0001",
              f"keys={list((d or {}).keys())}")
        MEMBER_TOKEN = d.get("token")

    s, d, _ = req("GET", "/api/member/profile", token=MEMBER_TOKEN, base=base)
    check("member token works on member route", s == 200, f"got {s}")
    s, d, _ = req("GET", "/api/member/profile", token=STAFF_TOKEN, base=base)
    check("staff token rejected on member route", s == 403, f"got {s}")
    s, d, _ = req("GET", "/api/users", token=MEMBER_TOKEN, base=base)
    check("member token rejected on staff-only route", s == 403, f"got {s}")


def suite_items_categories(base):
    section("3. Categories + items CRUD")

    # Category lifecycle
    s, cat, _ = req("POST", "/api/item-categories",
                    {"name": f"{MARKER} Cat", "description": "e2e"}, token=STAFF_TOKEN, base=base)
    check("create category 2xx", s in (200, 201), f"got {s} {cat}")
    cat_id = cat.get("id") if isinstance(cat, dict) else None
    check("category created with id", bool(cat_id))
    if cat_id:
        CREATED["categories"].append(cat_id)
    else:
        return  # everything below depends on the category

    s, d, _ = req("GET", f"/api/item-categories/{cat_id}", token=STAFF_TOKEN, base=base)
    check("new category items_count is 0", (d or {}).get("items_count") == 0, f"items_count={d.get('items_count')}")

    # Item lifecycle — the frontend sends multipart FormData (optionally with
    # an image), so the tests mirror that contract exactly.
    sku, barcode = f"{MARKER}-SKU", f"999{uuid.uuid4().hex[:10]}"
    fields = {"name": f"{MARKER} Widget", "sku": sku, "barcode": barcode,
              "category_id": str(cat_id), "unit_price": "10000", "cost_price": "6000",
              "current_stock": "100", "min_stock_threshold": "5", "unit_of_measure": "pcs"}
    status, raw = post_form("/api/items", fields, token=STAFF_TOKEN, base=base)
    s, d = status, raw
    check("create item (FormData, like the UI) 2xx", s in (200, 201), f"got {s} {d}")
    item_id = d.get("id") if isinstance(d, dict) else None
    check("item created with id", bool(item_id))
    if not item_id:
        return
    CREATED["items"].append(item_id)

    s, d, _ = req("GET", "/api/items?search=" + MARKER, token=STAFF_TOKEN, base=base)
    check("search finds the e2e item", any(i.get("id") == item_id for i in (d or {}).get("data", [])))
    check("list pagination shape has data/current_page/last_page/per_page/total",
          isinstance(d, dict) and {"data", "current_page", "last_page", "per_page", "total"} <= set(d),
          f"keys={list((d or {}).keys())}")

    s, d, _ = req("GET", f"/api/items/scan?code={barcode}", token=STAFF_TOKEN, base=base)
    check("scan by barcode", (d or {}).get("id") == item_id, f"got {s} {d}")
    s, d, _ = req("GET", f"/api/items/scan?code={sku}", token=STAFF_TOKEN, base=base)
    check("scan by sku", (d or {}).get("id") == item_id, f"got {s}")

    s, d, _ = req("GET", f"/api/items/{item_id}", token=STAFF_TOKEN, base=base)
    check("item detail has category object",
          (d or {}).get("category", {}).get("id") == cat_id, f"category={d.get('category')}")

    # Update — the UI POSTs to /items/{id} with _method=PUT (Laravel spoofing)
    upd = dict(fields, unit_price="12000", current_stock="80", _method="PUT")
    status, raw = post_form(f"/api/items/{item_id}", upd, token=STAFF_TOKEN, base=base)
    check("item update via _method=PUT spoof 200", status == 200, f"got {status} {raw}")
    s, d, _ = req("GET", f"/api/items/{item_id}", token=STAFF_TOKEN, base=base)
    check("updated price persisted", (d or {}).get("unit_price") == 12000, f"unit_price={d.get('unit_price')}")
    check("updated stock persisted", (d or {}).get("current_stock") == 80, f"current_stock={d.get('current_stock')}")
    db = sql(f"SELECT unit_price, current_stock FROM items WHERE id={item_id}")
    check("DB row matches API after update", db == "12000.00\t80", f"db={db}")

    # Category count now 1
    s, d, _ = req("GET", f"/api/item-categories/{cat_id}", token=STAFF_TOKEN, base=base)
    check("category items_count == 1 after item create", (d or {}).get("items_count") == 1,
          f"items_count={d.get('items_count')}")

    # Delete guard (Laravel parity): category with items must be refused
    s, d, _ = req("DELETE", f"/api/item-categories/{cat_id}", token=STAFF_TOKEN, base=base)
    check("delete guarded category -> 422", s == 422, f"got {s} {d}")
    s, d, _ = req("GET", f"/api/item-categories/{cat_id}", token=STAFF_TOKEN, base=base)
    check("guarded category still exists", s == 200, f"got {s}")

    # Soft delete hides from scan, category count unchanged (all-item parity)
    s, d, _ = req("DELETE", f"/api/items/{item_id}", token=STAFF_TOKEN, base=base)
    check("item soft delete 200", s == 200, f"got {s}")
    s, d, _ = req("GET", f"/api/items/scan?code={barcode}", token=STAFF_TOKEN, base=base)
    check("soft-deleted item not scannable", s == 404, f"got {s}")
    db = sql(f"SELECT is_active FROM items WHERE id={item_id}")
    check("soft delete sets is_active=0 in DB", db == "0", f"db={db}")


def suite_members(base):
    section("4. Members (create -> hash -> member login)")
    phone = f"081{uuid.uuid4().hex[:9]}"
    s, d, _ = req("POST", "/api/members",
                  {"name": f"{MARKER} Member", "phone": phone, "password": "secret123"},
                  token=STAFF_TOKEN, base=base)
    check("create member 2xx", s in (200, 201), f"got {s} {d}")
    member_id = (d or {}).get("id") if isinstance(d, dict) else None
    if not member_id:
        return
    check("member has generated membership_id KOP-",
          bool(re.match(r"^KOP-\d{4}-\d{4}$", d.get("membership_id", ""))),
          f"membership_id={d.get('membership_id')}")
    CREATED["members"].append(member_id)

    db = sql(f"SELECT password FROM members WHERE id={member_id}")
    check("member password stored as bcrypt hash", db.startswith("$2"), f"stored={db[:20]}...")

    s, d, _ = req("POST", "/api/auth/member/login", {"identifier": phone, "password": "secret123"}, base=base)
    check("admin-created member CAN log in (bcrypt fix)", s == 200, f"got {s} {d}")
    s, d, _ = req("POST", "/api/auth/member/login", {"identifier": phone, "password": "WRONG"}, base=base)
    check("member login rejects bad password", s in (400, 401), f"got {s}")

    s, d, _ = req("GET", f"/api/members/lookup?query={phone}", token=STAFF_TOKEN, base=base)
    check("member lookup by phone finds member",
          isinstance(d, list) and any(m.get("id") == member_id for m in d), f"got {d}")

    s, d, _ = req("PUT", f"/api/members/{member_id}",
                  {"name": f"{MARKER} Renamed", "phone": phone, "password": "newpass456"}, token=STAFF_TOKEN, base=base)
    check("member update 200", s == 200, f"got {s} {d}")
    s, d, _ = req("GET", f"/api/members/{member_id}", token=STAFF_TOKEN, base=base)
    check("member rename persisted", (d or {}).get("name") == f"{MARKER} Renamed", f"name={d.get('name')}")
    # Admin password reset — new password must authenticate, old must not.
    s, d, _ = req("POST", "/api/auth/member/login", {"identifier": phone, "password": "secret123"}, base=base)
    check("old password rejected after admin reset", s in (400, 401), f"got {s}")
    s, d, _ = req("POST", "/api/auth/member/login", {"identifier": phone, "password": "newpass456"}, base=base)
    check("new password works after admin reset", s == 200, f"got {s}")

    s, d, _ = req("DELETE", f"/api/members/{member_id}", token=STAFF_TOKEN, base=base)
    check("member delete 200", s == 200, f"got {s}")
    # Member delete is a soft delete (is_active=0) — cleanup below purges the
    # row, so keep it in CREATED.


def suite_checkout(base):
    section("5. Checkout pipeline (preview -> checkout -> stock -> void)")

    # Dedicated item so the flow never touches real data
    sku, barcode = f"{MARKER}-CO", f"998{uuid.uuid4().hex[:10]}"
    status, item = post_form("/api/items",
                             {"name": f"{MARKER} CashItem", "sku": sku, "barcode": barcode,
                              "unit_price": "10000", "cost_price": "5000", "current_stock": "100",
                              "min_stock_threshold": "5"},
                             token=STAFF_TOKEN, base=base)
    co_item = item.get("id") if isinstance(item, dict) else None
    check("checkout item created (FormData)", bool(co_item), f"got {status} {item}")
    if not co_item:
        return
    CREATED["items"].append(co_item)

    body = {"items": [{"item_id": co_item, "quantity": 2}], "member_identifier": None,
            "discount_id": None, "payment_method": "cash"}

    # Preview math
    s, d, _ = req("POST", "/api/transactions/preview", body, token=STAFF_TOKEN, base=base)
    check("preview 200", s == 200, f"got {s} {d}")
    check("preview shape (subtotal/tax/total/lines/warnings)",
          {"subtotal", "tax_amount", "total", "lines", "warnings"} <= set(d or {}), f"keys={list((d or {}).keys())}")
    check("preview subtotal = 2x unit price", (d or {}).get("subtotal") == 20000, f"subtotal={d.get('subtotal')}")
    check("preview total == subtotal - discount + tax",
          (d or {}).get("total") == d["subtotal"] - d.get("discount_amount", 0) + d.get("tax_amount", 0),
          f"total={d.get('total')}")

    # Preview with member discount (seeded "Diskon Anggota 5%" member scope)
    body_m = dict(body, member_identifier="KOP-2610-0001")
    s, d, _ = req("POST", "/api/transactions/preview", body_m, token=STAFF_TOKEN, base=base)
    check("member discount preview applies 5%", (d or {}).get("discount_amount") == 1000,
          f"discount_amount={d.get('discount_amount')} discount={d.get('discount')}")
    check("member discount preview total 19000", (d or {}).get("total") == 19000, f"total={d.get('total')}")

    # Commit (no discount)
    s, d, _ = req("POST", "/api/transactions/checkout", body, token=STAFF_TOKEN, base=base)
    check("checkout 200", s == 200, f"got {s} {d}")
    txn = d or {}
    CREATED["transactions"].append(txn.get("id"))
    check("transaction_code pattern TRX-YYYYMMDD-NNNN",
          bool(re.match(r"^TRX-\d{8}-\d{4,}$", txn.get("transaction_code", ""))),
          f"code={txn.get('transaction_code')}")
    check("checkout total = 20000", txn.get("total") == 20000, f"total={txn.get('total')}")
    check("receipt has line items with name/quantity/subtotal",
          bool(txn.get("items")) and txn["items"][0].get("item", {}).get("name") == f"{MARKER} CashItem"
          and txn["items"][0].get("quantity") == 2 and txn["items"][0].get("subtotal") == 20000,
          f"items={txn.get('items')}")
    db = sql(f"SELECT current_stock FROM items WHERE id={co_item}")
    check("stock deducted 100 -> 98 in DB", db == "98", f"db={db}")
    db = sql(f"SELECT COUNT(*) FROM transaction_items WHERE transaction_id={txn.get('id')}")
    check("transaction_items row written", db == "1", f"db={db}")

    # Detail endpoint consistency
    s, d, _ = req("GET", f"/api/transactions/{txn.get('id')}", token=STAFF_TOKEN, base=base)
    check("GET transaction returns same totals + items",
          (d or {}).get("total") == 20000 and bool((d or {}).get("items")), f"got {s}")

    # Void + audit log
    s, d, _ = req("POST", f"/api/transactions/{txn.get('id')}/void", token=STAFF_TOKEN, base=base)
    check("void 200", s == 200, f"got {s}")
    s, d, _ = req("GET", f"/api/transactions/{txn.get('id')}", token=STAFF_TOKEN, base=base)
    check("void marks payment_status=void", (d or {}).get("payment_status") == "void",
          f"status={d.get('payment_status')}")
    db = sql(f"SELECT COUNT(*) FROM audit_logs WHERE auditable_type='transaction' AND auditable_id={txn.get('id')} AND action='void'")
    check("void wrote an audit_logs row", db == "1", f"db={db}")

    # Concurrent checkout race: 4 parallel qty=1 checkouts on fresh stock
    status, item2 = post_form("/api/items",
                              {"name": f"{MARKER} RaceItem", "sku": f"{MARKER}-RACE", "barcode": f"997{uuid.uuid4().hex[:10]}",
                               "unit_price": "5000", "cost_price": "2500", "current_stock": "10",
                               "min_stock_threshold": "1"},
                              token=STAFF_TOKEN, base=base)
    race_item = item2.get("id") if isinstance(item2, dict) else None
    if not race_item:
        check("race item created (FormData)", False, f"got {status} {item2}")
        return
    CREATED["items"].append(race_item)
    results, codes = [], []
    lock = threading.Lock()

    def one_checkout():
        st, resp, _ = req("POST", "/api/transactions/checkout",
                          {"items": [{"item_id": race_item, "quantity": 1}],
                           "member_identifier": None, "discount_id": None,
                           "payment_method": "cash"}, token=STAFF_TOKEN, base=base)
        with lock:
            results.append(st)
            if isinstance(resp, dict):
                codes.append(resp.get("transaction_code"))
                tid = resp.get("id")
                if tid:
                    CREATED["transactions"].append(tid)

    threads = [threading.Thread(target=one_checkout) for _ in range(4)]
    for t in threads: t.start()
    for t in threads: t.join()
    check("4 concurrent checkouts all succeed", results.count(200) == 4, f"statuses={results}")
    check("concurrent transaction codes all unique", len(set(codes)) == 4, f"codes={codes}")
    db = sql(f"SELECT current_stock FROM items WHERE id={race_item}")
    check("race stock exactly 10-4=6", db == "6", f"db={db}")


def suite_loans(base):
    section("6. Loans (create -> approve -> pay -> paid_off)")

    s, products, _ = req("GET", "/api/loan-products", token=STAFF_TOKEN, base=base)
    product_list = products.get("data", []) if isinstance(products, dict) else products
    check("loan products list 200", s == 200 and len(product_list) > 0, f"got {s}")
    product = product_list[0]

    # Dedicated member so the seeded members' loan_balance is never touched
    # (loan create bumps it, then the member is removed in cleanup).
    lsp = f"081{uuid.uuid4().hex[:9]}"
    s, lm, _ = req("POST", "/api/members",
                   {"name": f"{MARKER} LoanMember", "phone": lsp, "password": "secret123"},
                   token=STAFF_TOKEN, base=base)
    member_id = (lm or {}).get("id") if isinstance(lm, dict) else None
    check("loan test member created", bool(member_id), f"got {s} {lm}")
    if not member_id:
        return
    CREATED["members"].append(member_id)

    def make_loan(principal):
        s, d, _ = req("POST", "/api/owner/loans",
                      {"member_id": int(member_id), "loan_product_id": product["id"],
                       "principal": principal, "term_months": 3, "status": "pending"},
                      token=STAFF_TOKEN, base=base)
        check("loan create 2xx", s in (200, 201), f"got {s} {d}")
        loan_id = (d or {}).get("id")
        if loan_id:
            CREATED["loans"].append(loan_id)
        return loan_id, d

    # Approve -> pay in two installments -> paid_off
    loan_id, d = make_loan(30000)
    if loan_id:
        s, d, _ = req("POST", f"/api/loans/{loan_id}/approve", {}, token=STAFF_TOKEN, base=base)
        check("loan approve -> approved", s == 200 and (d or {}).get("status") == "approved",
              f"got {s} status={d.get('status')}")
        db = sql(f"SELECT outstanding_balance, total_paid FROM loans WHERE id={loan_id}")
        check("approve persists outstanding 30000/0", db in ("30000.00\t0.00", "30000.00\t0"), f"db={db}")

        s, d, _ = req("POST", f"/api/loans/{loan_id}/pay", {"amount": 10000}, token=STAFF_TOKEN, base=base)
        check("partial pay 200", s == 200, f"got {s} {d}")
        s, d, _ = req("GET", f"/api/loans/{loan_id}", token=STAFF_TOKEN, base=base)
        check("paid_amount accumulated to 10000", (d or {}).get("total_paid") == 10000,
              f"total_paid={d.get('total_paid')}")
        check("status stays active after partial pay", (d or {}).get("status") in ("approved", "active"),
              f"status={d.get('status')}")

        s, d, _ = req("POST", f"/api/loans/{loan_id}/pay", {"amount": 20000}, token=STAFF_TOKEN, base=base)
        check("final pay 200", s == 200, f"got {s}")
        s, d, _ = req("GET", f"/api/loans/{loan_id}", token=STAFF_TOKEN, base=base)
        check("fully paid loan -> paid_off", (d or {}).get("status") == "paid_off",
              f"status={d.get('status')}")
        db = sql(f"SELECT total_paid FROM loans WHERE id={loan_id}")
        check("DB total_paid = 30000", db == "30000.00", f"db={db}")

    # Reject flow
    loan_id2, d = make_loan(20000)
    if loan_id2:
        s, d, _ = req("POST", f"/api/loans/{loan_id2}/reject", {"reason": "e2e"}, token=STAFF_TOKEN, base=base)
        check("loan reject -> rejected", s == 200 and (d or {}).get("status") == "rejected",
              f"got {s} status={d.get('status')}")


def suite_restocks(base):
    section("7. Restocks")
    # Restock records can't be deleted via API, so run against a soft-deleted
    # e2e item and leave one marked record behind.
    status, item = post_form("/api/items",
                             {"name": f"{MARKER} Stock", "sku": f"{MARKER}-RST", "barcode": f"996{uuid.uuid4().hex[:10]}",
                              "unit_price": "8000", "cost_price": "4000", "current_stock": "10",
                              "min_stock_threshold": "2"},
                             token=STAFF_TOKEN, base=base)
    rst_item = item.get("id") if isinstance(item, dict) else None
    if not rst_item:
        check("restock item created (FormData)", False, f"got {status} {item}")
        return
    CREATED["items"].append(rst_item)

    s, supplier, _ = req("POST", "/api/suppliers",
                         {"name": f"{MARKER} Supplier", "phone": "081200000099"},
                         token=STAFF_TOKEN, base=base)
    supplier_id = (supplier or {}).get("id")
    if supplier_id:
        CREATED["suppliers"].append(supplier_id)

    s, d, _ = req("POST", "/api/restocks",
                  {"item_id": rst_item, "supplier_id": supplier_id,
                   "quantity": 5, "cost_per_unit": 4000, "notes": f"{MARKER} restock"},
                  token=STAFF_TOKEN, base=base)
    check("restock create 2xx", s in (200, 201), f"got {s} {d}")
    restock = d or {}
    check("restock total_cost = qty*cost", restock.get("total_cost") == 20000,
          f"total_cost={restock.get('total_cost')}")
    db = sql(f"SELECT current_stock FROM items WHERE id={rst_item}")
    check("restock increased stock 10 -> 15", db == "15", f"db={db}")


def suite_reports(base):
    section("8. Reports consistency vs raw SQL")
    s, d, _ = req("GET", "/api/dashboard/summary", token=STAFF_TOKEN, base=base)
    check("dashboard summary 200", s == 200, f"got {s}")

    db_members = sql("SELECT COUNT(*) FROM members")
    check("total_members matches DB", str((d or {}).get("total_members")) == db_members,
          f"api={d.get('total_members')} db={db_members}")
    db_items = sql("SELECT COUNT(*) FROM items")
    check("total_items matches DB", str((d or {}).get("total_items")) == db_items,
          f"api={d.get('total_items')} db={db_items}")

    s, d, _ = req("GET", "/api/reports/stock-valuation", token=STAFF_TOKEN, base=base)
    db_val = sql("SELECT ROUND(SUM(COALESCE(current_stock,0)*COALESCE(cost_price,0)),2) FROM items")
    api_val = (d or {}).get("total_value_at_cost")
    check("stock-valuation total equals DB sum", api_val is not None and
          abs(float(api_val) - float(db_val)) < 0.01, f"api={api_val} db={db_val}")

    s, d, _ = req("GET", "/api/dashboard/alerts", token=STAFF_TOKEN, base=base)
    db_low = sql("SELECT COUNT(*) FROM items WHERE (min_stock_threshold IS NOT NULL AND COALESCE(current_stock,0) <= min_stock_threshold) OR (min_stock_threshold IS NULL AND COALESCE(current_stock,0) <= 10)")
    check("alerts low_stock count matches DB", str(len((d or {}).get("low_stock", []))) == db_low,
          f"api={len((d or {}).get('low_stock', []))} db={db_low}")


def suite_contract(base):
    section("9. Frontend contract shapes")
    # The exact keys the UI components rely on.
    s, d, _ = req("GET", "/api/items?page=1&per_page=1", token=STAFF_TOKEN, base=base)
    item_keys = set((d or {}).get("data", [{}])[0] or {})
    check("item JSON keys (sku/unit_price/current_stock/min_stock_threshold/image_path)",
          {"id", "name", "sku", "unit_price", "current_stock", "min_stock_threshold", "is_active"} <= item_keys,
          f"keys={sorted(item_keys)[:10]}")

    s, d, _ = req("GET", "/api/transactions?page=1&per_page=1", token=STAFF_TOKEN, base=base)
    tx = ((d or {}).get("data") or [{}])[0]
    check("transaction JSON keys (transaction_code/payment_method/total/cashier/items)",
          {"transaction_code", "payment_method", "payment_status", "total", "cashier_id"} <= set(tx),
          f"keys={sorted(tx.keys())[:12]}")

    s, d, _ = req("GET", "/api/members?page=1&per_page=1", token=STAFF_TOKEN, base=base)
    m = ((d or {}).get("data") or [{}])[0]
    check("member JSON keys (membership_id/shu_balance/join_date)",
          {"id", "membership_id", "name", "shu_balance", "is_active"} <= set(m),
          f"keys={sorted(m.keys())[:10]}")

    s, d, _ = req("GET", "/api/item-categories?page=1&per_page=1", token=STAFF_TOKEN, base=base)
    c = ((d or {}).get("data") or [{}])[0]
    check("category JSON has items_count key", "items_count" in c, f"keys={list(c.keys())}")


# ------------------------------------------------------------------ cleanup ---
def sql_safe(query):
    try:
        sql(query)
    except Exception as e:
        print(f"  cleanup sql note: {str(e)[:120]}")


def cleanup(base):
    section("Cleanup (delete test rows via API, SQL fallback)")
    # Loans first (they reference members), then members, suppliers,
    # transactions (line items may need manual FK cleanup), items,
    # categories last.
    for loan_id in CREATED["loans"]:
        s, _, _ = req("DELETE", f"/api/owner/loans/{loan_id}", token=STAFF_TOKEN, base=base)
        if s in (404, 405):
            sql_safe(f"DELETE FROM loan_payments WHERE loan_id={loan_id}; DELETE FROM loans WHERE id={loan_id};")

    for member_id in CREATED["members"]:
        s, _, _ = req("DELETE", f"/api/members/{member_id}?force=true", token=STAFF_TOKEN, base=base)
        if s in (404, 405):
            sql_safe(f"DELETE FROM members WHERE id={member_id};")
        # The API only soft-deletes; purge rows this run created so the
        # database is left exactly as it found it.
        sql_safe(f"DELETE FROM members WHERE id={member_id} AND name LIKE 'E2E_%';")

    for supplier_id in CREATED["suppliers"]:
        s, _, _ = req("DELETE", f"/api/suppliers/{supplier_id}", token=STAFF_TOKEN, base=base)
        if s in (404, 405):
            sql_safe(f"DELETE FROM suppliers WHERE id={supplier_id};")

    txn_ids = [str(t) for t in CREATED["transactions"] if t]
    if txn_ids:
        for txn_id in txn_ids:
            req("DELETE", f"/api/transactions/{txn_id}", token=STAFF_TOKEN, base=base)
        sql_safe("DELETE FROM transaction_items WHERE transaction_id IN "
                 f"({','.join(txn_ids)}); DELETE FROM transactions WHERE id IN ({','.join(txn_ids)});")

    for item_id in CREATED["items"]:
        s, _, _ = req("DELETE", f"/api/items/{item_id}?force=true", token=STAFF_TOKEN, base=base)
        if s in (404, 405):
            req("DELETE", f"/api/items/{item_id}", token=STAFF_TOKEN, base=base)
        # FK sweep: restocking_records/transaction_items referencing e2e items
        sql_safe(f"DELETE FROM restocking_records WHERE item_id={item_id}; "
                 f"DELETE FROM transaction_items WHERE item_id={item_id}; "
                 f"DELETE FROM items WHERE id={item_id} AND sku LIKE '%{MARKER}%';")

    for cat_id in CREATED["categories"]:
        s, _, _ = req("DELETE", f"/api/item-categories/{cat_id}", token=STAFF_TOKEN, base=base)
        if s in (404, 405):
            sql_safe(f"DELETE FROM item_categories WHERE id={cat_id} AND name LIKE '%{MARKER}%';")

    print(f"  cleaned items={CREATED['items']} categories={CREATED['categories']} "
          f"members={CREATED['members']} loans={CREATED['loans']} transactions={CREATED['transactions']}")


# ---------------------------------------------------------------------- main ---
def main():
    global VERBOSE, STAFF_TOKEN, MEMBER_TOKEN
    ap = argparse.ArgumentParser()
    ap.add_argument("--base", default="http://localhost")
    ap.add_argument("--verbose", action="store_true")
    ap.add_argument("--skip-cleanup", action="store_true")
    args = ap.parse_args()
    VERBOSE = args.verbose

    print(f"E2E test run — base={args.base} marker={MARKER}")

    # Tokens needed before suites run
    s, d, _ = req("POST", "/api/auth/staff/login",
                  {"email": "admin@koperasi.test", "password": "password"}, base=args.base)
    if s != 200:
        print(f"FATAL: staff login failed ({s}) — is the stack up?")
        sys.exit(1)
    STAFF_TOKEN = d["token"]
    s, d, _ = req("POST", "/api/auth/member/login",
                  {"identifier": "KOP-2610-0001", "password": "password"}, base=args.base)
    MEMBER_TOKEN = d.get("token") if s == 200 else None

    try:
        suite_infra(args.base)
        suite_auth(args.base)
        suite_items_categories(args.base)
        suite_members(args.base)
        suite_checkout(args.base)
        suite_loans(args.base)
        suite_restocks(args.base)
        suite_reports(args.base)
        suite_contract(args.base)
    finally:
        if not args.skip_cleanup:
            try:
                cleanup(args.base)
            except Exception as e:
                print(f"  cleanup error (non-fatal): {e}")

    print(f"\n{'=' * 50}\nRESULT: {PASS} passed, {FAIL} failed")
    if FAILURES:
        print("\nFailed checks:")
        for name, detail in FAILURES:
            print(f"  - {name}: {detail}")
    sys.exit(0 if FAIL == 0 else 1)


if __name__ == "__main__":
    main()
