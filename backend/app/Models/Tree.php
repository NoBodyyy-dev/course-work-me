<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Relations\HasMany;
use Illuminate\Database\Eloquent\Model;

class Tree extends Model
{
    protected $fillable = [
        'title',
        'orientation',
        'max_depth',
        'child_probability',
        'seed',
        'node_count',
        'generated_at',
    ];

    protected function casts(): array
    {
        return [
            'child_probability' => 'float',
            'generated_at' => 'datetime',
        ];
    }

    public function nodes(): HasMany
    {
        return $this->hasMany(TreeNode::class)->orderBy('path');
    }
}
