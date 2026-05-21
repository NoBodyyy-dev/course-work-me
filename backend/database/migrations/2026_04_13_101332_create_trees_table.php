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
        Schema::create('trees', function (Blueprint $table) {
            $table->id();
            $table->string('title')->nullable();
            $table->string('orientation', 16)->default('vertical');
            $table->unsignedTinyInteger('max_depth');
            $table->decimal('child_probability', 3, 2)->default(0.60);
            $table->unsignedBigInteger('seed');
            $table->unsignedInteger('node_count')->default(0);
            $table->timestamp('generated_at');
            $table->timestamps();
        });
    }

    /**
     * Reverse the migrations.
     */
    public function down(): void
    {
        Schema::dropIfExists('trees');
    }
};
