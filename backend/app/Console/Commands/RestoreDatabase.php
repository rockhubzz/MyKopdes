<?php

namespace App\Console\Commands;

use Illuminate\Console\Command;
use Illuminate\Support\Facades\Storage;

class RestoreDatabase extends Command
{
    protected $signature = 'koperasi:restore {filename : File under storage/app/backups, e.g. koperasi_backup_20260101_020000.sql.gz}';
    protected $description = 'Restore the MySQL database from a previously generated backup';

    public function handle(): int
    {
        $filename = $this->argument('filename');

        if (! Storage::disk('backups')->exists($filename)) {
            $this->error("Backup file not found: {$filename}");
            return self::FAILURE;
        }

        if (! $this->confirm('This will OVERWRITE the current database. Continue?', false) && $this->input->isInteractive()) {
            $this->warn('Restore cancelled.');
            return self::SUCCESS;
        }

        $connection = config('database.connections.'.config('database.default'));
        $path = Storage::disk('backups')->path($filename);

        $host = escapeshellarg($connection['host']);
        $port = escapeshellarg((string) $connection['port']);
        $user = escapeshellarg($connection['username']);
        $pass = $connection['password'];
        $db = escapeshellarg($connection['database']);
        $src = escapeshellarg($path);

        $command = "gunzip < {$src} | MYSQL_PWD=".$this->quoteEnv($pass)." mysql -h {$host} -P {$port} -u {$user} {$db}";

        $this->info('Restoring...');
        exec($command, $output, $exitCode);

        if ($exitCode !== 0) {
            $this->error('Restore failed.');
            return self::FAILURE;
        }

        $this->info('Restore complete.');
        return self::SUCCESS;
    }

    private function quoteEnv(string $value): string
    {
        return "'".str_replace("'", "'\\''", $value)."'";
    }
}
