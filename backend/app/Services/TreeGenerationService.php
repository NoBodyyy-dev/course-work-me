<?php

namespace App\Services;

use App\Models\Tree;
use App\Models\TreeNode;
use Illuminate\Support\Carbon;
use Illuminate\Support\Facades\DB;
use Random\Engine\Mt19937;
use Random\Randomizer;

class TreeGenerationService
{
    private int $nextNodeNumber = 1;

    public function generate(array $payload): Tree
    {
        $orientation = $payload['orientation'] ?? 'vertical';
        $maxDepth = (int) ($payload['max_depth'] ?? 4);
        $childProbability = round((float) ($payload['child_probability'] ?? 0.6), 2);
        $seed = (int) ($payload['seed'] ?? random_int(1, PHP_INT_MAX));

        return DB::transaction(function () use ($payload, $orientation, $maxDepth, $childProbability, $seed) {
            $randomizer = new Randomizer(new Mt19937($seed));

            $tree = Tree::query()->create([
                'title' => $payload['title'] ?? 'Сгенерированное дерево',
                'orientation' => $orientation,
                'max_depth' => $maxDepth,
                'child_probability' => $childProbability,
                'seed' => $seed,
                'generated_at' => Carbon::now(),
            ]);

            $this->nextNodeNumber = 1;
            $this->createNode(
                tree: $tree,
                parent: null,
                depth: 0,
                childSlot: null,
                path: '1',
                randomizer: $randomizer,
            );

            $tree->forceFill([
                'node_count' => $this->nextNodeNumber - 1,
            ])->save();

            return $tree->fresh('nodes');
        });
    }

    private function createNode(
        Tree $tree,
        ?TreeNode $parent,
        int $depth,
        ?int $childSlot,
        string $path,
        Randomizer $randomizer
    ): TreeNode {
        $node = $tree->nodes()->create([
            'parent_id' => $parent?->id,
            'node_number' => $this->nextNodeNumber++,
            'depth' => $depth,
            'child_slot' => $childSlot,
            'path' => $path,
            'child_numbers' => [],
        ]);

        if ($depth >= $tree->max_depth) {
            return $node;
        }

        $childrenToCreate = collect([1, 2, 3])
            ->filter(fn () => $this->shouldCreateChild($tree->child_probability, $randomizer))
            ->values();

        if ($depth === 0 && $childrenToCreate->isEmpty()) {
            $childrenToCreate = collect([$randomizer->getInt(1, 3)]);
        }

        $children = [];

        foreach ($childrenToCreate as $slot) {
            $children[] = $this->createNode(
                tree: $tree,
                parent: $node,
                depth: $depth + 1,
                childSlot: $slot,
                path: "{$path}.{$slot}",
                randomizer: $randomizer,
            );
        }

        $node->forceFill([
            'child_numbers' => array_map(
                static fn (TreeNode $child): int => $child->node_number,
                $children,
            ),
        ])->save();

        return $node;
    }

    private function shouldCreateChild(float $probability, Randomizer $randomizer): bool
    {
        return $randomizer->getInt(1, 100) <= (int) round($probability * 100);
    }
}
