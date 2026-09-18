<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\AuditLog;
use Illuminate\Http\Request;

class AuditLogController extends Controller
{
    public function index(Request $request)
    {
        $query = AuditLog::with('actor')->latest('created_at');

        if ($request->filled('action')) {
            $query->where('action', 'like', '%'.$request->string('action').'%');
        }
        if ($request->filled('actor_id')) {
            $query->where('actor_id', $request->integer('actor_id'));
        }
        if ($request->filled('from')) {
            $query->whereDate('created_at', '>=', $request->date('from'));
        }
        if ($request->filled('to')) {
            $query->whereDate('created_at', '<=', $request->date('to'));
        }

        return $query->paginate($request->integer('per_page', 50));
    }

    public function show(AuditLog $auditLog)
    {
        return response()->json($auditLog->load('actor'));
    }
}
