<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Relations\BelongsTo;
use Illuminate\Database\Eloquent\Relations\HasMany;
use Illuminate\Database\Eloquent\Model;

class TreeNode extends Model
{
    protected $fillable = [
        'tree_id',
        'parent_id',
        'node_number',
        'depth',
        'child_slot',
        'path',
        'child_numbers',
    ];

    protected function casts(): array
    {
        return [
            'child_numbers' => 'array',
        ];
    }

    public function tree(): BelongsTo
    {
        return $this->belongsTo(Tree::class);
    }

    public function parent(): BelongsTo
    {
        return $this->belongsTo(TreeNode::class, 'parent_id');
    }

    public function children(): HasMany
    {
        return $this->hasMany(TreeNode::class, 'parent_id')->orderBy('child_slot');
    }
}
