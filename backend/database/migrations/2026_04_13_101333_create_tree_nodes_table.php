<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    /**
     * Run the migrations.
     */
    public function up(): void
    {
        Schema::create('tree_nodes', function (Blueprint $table) {
            $table->id();
            $table->foreignId('tree_id')->constrained()->cascadeOnDelete();
            $table->foreignId('parent_id')->nullable()->constrained('tree_nodes')->cascadeOnDelete();
            $table->unsignedInteger('node_number');
            $table->unsignedTinyInteger('depth');
            $table->unsignedTinyInteger('child_slot')->nullable();
            $table->string('path');
            $table->timestamps();

            $table->unique(['tree_id', 'node_number']);
            $table->unique(['tree_id', 'path']);
        });
    }

    /**
     * Reverse the migrations.
     */
    public function down(): void
    {
        Schema::dropIfExists('tree_nodes');
    }
};
