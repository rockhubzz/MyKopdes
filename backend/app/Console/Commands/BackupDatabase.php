<?php

namespace App\Console\Commands;

use Illuminate\Console\Command;
use Illuminate\Support\Facades\Storage;

class BackupDatabase extends Command
{
    protected $signature = 'koperasi:backup';
    protected $description = 'Dump the MySQL database to storage/app/backups as a gzip-compressed .sql.gz file';

    public function handle(): int
    {
        $connection = config('database.connections.'.config('database.default'));
        $filename = 'koperasi_backup_'.now()->format('Ymd_His').'.sql.gz';

        Storage::disk('backups')->makeDirectory('');
        $path = Storage::disk('backups')->path($filename);

        $host = escapeshellarg($connection['host']);
        $port = escapeshellarg((string) $connection['port']);
        $user = escapeshellarg($connection['username']);
        $pass = $connection['password'];
        $db = escapeshellarg($connection['database']);
        $dest = escapeshellarg($path);

        // mysqldump piped through gzip. Password is passed via env var
        // (MYSQL_PWD) rather than the command line so it doesn't show up in
        // `ps aux` on the host.
        $command = "MYSQL_PWD={$this->quoteEnv($pass)} mysqldump -h {$host} -P {$port} -u {$user} {$db} | gzip > {$dest}";

        $this->info('Starting backup...');
        exec($command, $output, $exitCode);

        if ($exitCode !== 0 || ! file_exists($path)) {
            $this->error('Backup failed. Ensure the mysql-client tools are installed in this container.');
            return self::FAILURE;
        }

        $this->info("Backup written to storage/app/backups/{$filename} (".round(filesize($path) / 1024, 1).' KB).');

        $this->pruneOldBackups();

        return self::SUCCESS;
    }

    private function quoteEnv(string $value): string
    {
        return "'".str_replace("'", "'\\''", $value)."'";
    }

    /** Keep the most recent 30 backups so the disk doesn't grow unbounded. */
    private function pruneOldBackups(): void
    {
        $files = collect(Storage::disk('backups')->files())
            ->filter(fn ($f) => str_ends_with($f, '.sql.gz'))
            ->sortByDesc(fn ($f) => Storage::disk('backups')->lastModified($f))
            ->values();

        foreach ($files->slice(30) as $old) {
            Storage::disk('backups')->delete($old);
        }
    }
}
