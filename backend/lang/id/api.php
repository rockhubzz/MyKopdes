<?php

/**
 * Pesan error API yang tampil ke pengguna, dalam Bahasa Indonesia.
 * Key harus sama persis dengan lang/en/api.php.
 */
return [
    'auth' => [
        'credentials' => 'Kredensial ini tidak cocok dengan data kami.',
        'staff_inactive' => 'Akun ini telah dinonaktifkan. Hubungi administrator.',
        'member_inactive' => 'Keanggotaan ini tidak aktif. Hubungi kantor koperasi.',
    ],
    'role' => [
        'forbidden' => 'Anda tidak memiliki izin untuk mengakses sumber daya ini.',
        'deactivated' => 'Akun Anda telah dinonaktifkan. Hubungi administrator.',
    ],
    'users' => [
        'self_deactivate' => 'Anda tidak dapat menonaktifkan akun Anda sendiri.',
        'force_blocked' => 'Tidak dapat menghapus permanen akun staf yang memiliki riwayat transaksi atau restok. Nonaktifkan saja.',
    ],
    'members' => [
        'force_blocked' => 'Tidak dapat menghapus permanen anggota yang memiliki riwayat pembelian. Nonaktifkan keanggotaannya.',
    ],
];
