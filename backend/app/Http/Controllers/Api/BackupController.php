<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\AuditLog;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\Artisan;
use Illuminate\Support\Facades\Storage;

class BackupController extends Controller
{
    public function index()
    {
        $files = collect(Storage::disk('backups')->files())
            ->filter(fn ($f) => str_ends_with($f, '.sql.gz'))
            ->map(fn ($f) => [
                'name' => basename($f),
                'size_bytes' => Storage::disk('backups')->size($f),
                'created_at' => Storage::disk('backups')->lastModified($f),
            ])
            ->sortByDesc('created_at')
            ->values();

        return response()->json($files);
    }

    public function store(Request $request)
    {
        Artisan::call('koperasi:backup');

        AuditLog::create([
            'actor_type' => \App\Models\User::class,
            'actor_id' => $request->user('staff')->id,
            'action' => 'backup_triggered',
            'ip_address' => $request->ip(),
            'created_at' => now(),
        ]);

        return response()->json(['message' => 'Backup started.', 'output' => Artisan::output()]);
    }

    public function download(string $filename)
    {
        abort_unless(preg_match('/^[\w\-.]+\.sql\.gz$/', $filename), 422);
        abort_unless(Storage::disk('backups')->exists($filename), 404);

        return Storage::disk('backups')->download($filename);
    }

    /**
     * Restores the DB from a previously generated backup file. This is
     * destructive by nature (per the brief's "manage backup/restore of the
     * database from the UI"), so it is admin-only at the route level and
     * requires the filename to already exist under the backups disk rather
     * than accepting an arbitrary upload path.
     */
    public function restore(Request $request)
    {
        $data = $request->validate(['filename' => ['required', 'string']]);
        abort_unless(preg_match('/^[\w\-.]+\.sql\.gz$/', $data['filename']), 422);
        abort_unless(Storage::disk('backups')->exists($data['filename']), 404, 'Backup file not found.');

        Artisan::call('koperasi:restore', ['filename' => $data['filename']]);

        AuditLog::create([
            'actor_type' => \App\Models\User::class,
            'actor_id' => $request->user('staff')->id,
            'action' => 'backup_restored',
            'changes' => ['filename' => $data['filename']],
            'ip_address' => $request->ip(),
            'created_at' => now(),
        ]);

        return response()->json(['message' => 'Restore completed.', 'output' => Artisan::output()]);
    }
}
