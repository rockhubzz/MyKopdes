FROM php:8.4-fpm-alpine AS base

RUN apk add --no-cache \
      bash git curl icu-dev oniguruma-dev libzip-dev zip unzip \
      libpng-dev freetype-dev jpeg-dev mysql-client \
    && docker-php-ext-configure gd --with-freetype --with-jpeg \
    && docker-php-ext-install -j$(nproc) pdo pdo_mysql mbstring intl zip gd bcmath opcache

COPY --from=composer:2 /usr/bin/composer /usr/bin/composer

WORKDIR /var/www/html

# ---- development target ---------------------------------------------------
FROM base AS development
COPY . /var/www/html
RUN composer install --no-interaction --no-progress --no-security-blocking
RUN chown -R www-data:www-data /var/www/html/storage /var/www/html/bootstrap/cache 2>/dev/null || true
COPY docker-entrypoint.sh /usr/local/bin/docker-entrypoint.sh
RUN chmod +x /usr/local/bin/docker-entrypoint.sh
ENTRYPOINT ["docker-entrypoint.sh"]
CMD ["php-fpm"]

# ---- production target -----------------------------------------------------
FROM base AS production
COPY . /var/www/html
RUN composer install --no-interaction --no-progress --no-security-blocking --optimize-autoloader --no-dev
RUN chown -R www-data:www-data /var/www/html/storage /var/www/html/bootstrap/cache 2>/dev/null || true
RUN php artisan config:cache || true
# The public/storage symlink is gitignored, so link it at startup (not build
# time) where the storage volume is actually mounted.
CMD ["sh", "-c", "php artisan storage:link || true; exec php-fpm"]
