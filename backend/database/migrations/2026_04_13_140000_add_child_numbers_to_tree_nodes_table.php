<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::table('tree_nodes', function (Blueprint $table) {
            $table->json('child_numbers')->nullable()->after('path');
        });
    }

    public function down(): void
    {
        Schema::table('tree_nodes', function (Blueprint $table) {
            $table->dropColumn('child_numbers');
        });
    }
};
