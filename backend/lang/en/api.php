<?php

/**
 * User-facing API error messages in English.
 *
 * These are resolved through the app locale (see SetLocale middleware, fed
 * by the frontend's Accept-Language header / koperasi_lang cookie), so the
 * same endpoint answers in the language the requester actually uses.
 * Keys must stay in sync with lang/id/api.php.
 */
return [
    'auth' => [
        'credentials' => 'These credentials do not match our records.',
        'staff_inactive' => 'This account has been deactivated. Contact an administrator.',
        'member_inactive' => 'This membership is inactive. Contact the cooperative office.',
    ],
    'role' => [
        'forbidden' => 'You do not have permission to access this resource.',
        'deactivated' => 'Your account has been deactivated. Contact an administrator.',
    ],
    'users' => [
        'self_deactivate' => 'You cannot deactivate your own account.',
        'force_blocked' => 'Cannot permanently delete a staff account with transaction or restock history. Deactivate it instead.',
    ],
    'members' => [
        'force_blocked' => 'Cannot permanently delete a member with purchase history. Deactivate the membership instead.',
    ],
];
