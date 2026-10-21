export type StaffRole = 'admin' | 'shop_owner' | 'employee';
export type Role = StaffRole | 'member';

export interface StaffUser {
  id: number;
  name: string;
  email: string;
  role: StaffRole;
  phone: string | null;
  shift_label: string | null;
  avatar_path: string | null;
  locale: string;
  is_active: boolean;
}

export interface Member {
  id: number;
  membership_id: string;
  name: string;
  email: string | null;
  phone: string | null;
  address: string | null;
  avatar_path: string | null;
  locale: string;
  join_date: string;
  shu_balance: string;
  is_active: boolean;
}

export interface ItemCategory {
  id: number;
  name: string;
  description: string | null;
  items_count?: number;
}

export interface Item {
  id: number;
  name: string;
  sku: string;
  barcode: string | null;
  category_id: number | null;
  category?: ItemCategory | null;
  unit_price: string;
  cost_price: string;
  unit_of_measure: string;
  image_path: string | null;
  current_stock: number;
  min_stock_threshold: number;
  expiry_date: string | null;
  is_active: boolean;
}

export interface Supplier {
  id: number;
  name: string;
  contact_person: string | null;
  phone: string | null;
  email: string | null;
  address: string | null;
  notes: string | null;
}

export interface RestockingRecord {
  id: number;
  item_id: number;
  item?: Item;
  supplier_id: number | null;
  supplier?: Supplier | null;
  quantity: number;
  cost_per_unit: string;
  total_cost: string;
  /**
   * The FK attribute AND the eager-loaded `submittedBy` relation both
   * serialize to the `submitted_by` JSON key (Laravel snake_cases relation
   * names), so when the relation is loaded this arrives as the staff object
   * instead of the id. Accept both shapes; the detail view normalizes.
   */
  submitted_by: number | StaffUser;
  submittedBy?: StaffUser;
  notes: string | null;
  restocked_at: string;
}

export type DiscountType = 'percentage' | 'flat' | 'buy_x_get_y';
export type DiscountScope = 'general' | 'member';

export interface Discount {
  id: number;
  name: string;
  description: string | null;
  type: DiscountType;
  value: string;
  buy_qty: number | null;
  get_qty: number | null;
  scope: DiscountScope;
  category_id: number | null;
  category?: ItemCategory | null;
  min_purchase: string | null;
  is_active: boolean;
  starts_at: string | null;
  ends_at: string | null;
}

export type PaymentMethod = 'cash' | 'qris' | 'bank_transfer';
export type PaymentStatus = 'paid' | 'pending' | 'void';

export interface TransactionItem {
  id: number;
  item_id: number;
  item?: Item;
  quantity: number;
  unit_price: string;
  subtotal: string;
}

export interface Transaction {
  id: number;
  transaction_code: string;
  member_id: number | null;
  member?: Member | null;
  cashier_id: number;
  cashier?: StaffUser;
  subtotal: string;
  discount_id: number | null;
  discount?: Discount | null;
  discount_amount: string;
  tax_amount: string;
  total: string;
  payment_method: PaymentMethod;
  payment_status: PaymentStatus;
  paid_at: string | null;
  created_at: string;
  items?: TransactionItem[];
}

export interface Paginated<T> {
  data: T[];
  current_page: number;
  last_page: number;
  per_page: number;
  total: number;
}

/** Read-only price quote from POST /transactions/preview — same math as
 *  checkout, no writes. Amounts are authoritative (server-computed). */
export interface CheckoutPreviewLine {
  item_id: number;
  name: string;
  quantity: number;
  unit_price: number;
  subtotal: number;
}

export interface CheckoutPreview {
  lines: CheckoutPreviewLine[];
  subtotal: number;
  discount: Discount | null;
  discount_amount: number;
  tax_rate: number;
  tax_amount: number;
  total: number;
  member: { id: number; name: string; membership_id: string } | null;
  warnings: string[];
}

export interface AuditLog {
  id: number;
  actor_type: string | null;
  actor_id: number | null;
  actor?: { name?: string; email?: string } | null;
  action: string;
  auditable_type: string | null;
  auditable_id: number | null;
  changes: Record<string, unknown> | null;
  ip_address: string | null;
  created_at: string;
}

// ─── Loans ───────────────────────────────────────────────────────────────────

export type LoanStatus = 'draft' | 'pending' | 'approved' | 'rejected' | 'active' | 'paid_off' | 'written_off';

export interface LoanProduct {
  id: number;
  name: string;
  description: string | null;
  interest_rate: string;        // decimal:4, e.g. "0.0100"
  interest_period: 'monthly' | 'annual';
  min_term_months: number;
  max_term_months: number;
  min_amount: string;
  max_amount: string | null;
  processing_fee_rate: string;  // decimal:4
  eligibility_rule: string | null;
  is_active: boolean;
  created_at: string;
  updated_at: string;
}

export interface LoanPayment {
  id: number;
  loan_id: number;
  recorded_by: number | null;
  recordedBy?: StaffUser | null;
  amount: string;
  payment_method: PaymentMethod;
  paid_at: string | null;
  notes: string | null;
  created_at: string;
  updated_at: string;
}

export interface Loan {
  id: number;
  loan_code: string;
  member_id: number;
  member?: Member;
  loan_product_id: number;
  product?: LoanProduct;
  approved_by: number | null;
  approvedBy?: StaffUser | null;
  principal: string;
  interest_rate: string;
  interest_period: 'monthly' | 'annual';
  term_months: number;
  processing_fee: string;
  outstanding_balance: string;
  accrued_interest: string;
  total_paid: string;
  status: LoanStatus;
  applied_at: string | null;
  approved_at: string | null;
  first_due_at: string | null;
  due_at: string | null;
  paid_off_at: string | null;
  notes: string | null;
  created_at: string;
  updated_at: string;
  payments?: LoanPayment[];
  minimum_installment?: number;  // computed on the backend when shown
}

export interface LoanSummary {
  total_outstanding: number;
  active_count: number;
  pending_count: number;
}

export interface MemberLoanIndex {
  loans: Paginated<Loan>;
  products: LoanProduct[];
}
