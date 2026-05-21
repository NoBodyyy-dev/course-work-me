<?php

namespace App\Services;

use App\Models\Tree;
use App\Models\TreeNode;
use Illuminate\Support\Collection;

class TreeResponseBuilder
{
    public function build(Tree $tree): array
    {
        $tree->loadMissing('nodes');

        $nodes = $tree->nodes->sortBy('path')->values();
        $groupedByParent = $nodes->groupBy(fn (TreeNode $node) => $node->parent_id ?? 'root');
        $root = $nodes->firstWhere('parent_id', null);

        return [
            'id' => $tree->id,
            'title' => $tree->title,
            'orientation' => $tree->orientation,
            'max_depth' => $tree->max_depth,
            'child_probability' => $tree->child_probability,
            'seed' => $tree->seed,
            'node_count' => $tree->node_count,
            'generated_at' => $tree->generated_at?->toIso8601String(),
            'root' => $root ? $this->buildNode($root, $groupedByParent) : null,
        ];
    }

    private function buildNode(TreeNode $node, Collection $groupedByParent): array
    {
        $children = $groupedByParent
            ->get($node->id, collect())
            ->sortBy('child_slot')
            ->values();

        return [
            'id' => $node->id,
            'number' => $node->node_number,
            'label' => 'V'.$node->node_number,
            'depth' => $node->depth,
            'path' => $node->path,
            'child_slot' => $node->child_slot,
            'child_numbers' => $node->child_numbers ?? $children->pluck('node_number')->all(),
            'children' => $children
                ->map(fn (TreeNode $child) => $this->buildNode($child, $groupedByParent))
                ->all(),
        ];
    }
}
