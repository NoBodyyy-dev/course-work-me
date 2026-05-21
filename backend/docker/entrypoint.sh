#!/bin/sh
set -eu

cd /var/www/html

if [ ! -f .env ]; then
  cp .env.example .env
fi

if [ ! -f vendor/autoload.php ]; then
  composer install --no-interaction --prefer-dist
fi

until php -r '
  $host = getenv("DB_HOST") ?: "postgres";
  $port = (int) (getenv("DB_PORT") ?: 5432);
  $connection = @fsockopen($host, $port, $errno, $errstr, 2);
  if ($connection) {
    fclose($connection);
    exit(0);
  }
  exit(1);
' >/dev/null 2>&1; do
  echo "Waiting for PostgreSQL..."
  sleep 2
done

php artisan key:generate --force
php artisan migrate --force
php artisan serve --host=0.0.0.0 --port=8000
