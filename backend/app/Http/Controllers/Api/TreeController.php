<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\Tree;
use App\Services\TreeGenerationService;
use App\Services\TreeResponseBuilder;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;

class TreeController extends Controller
{
    public function index(Request $request): JsonResponse
    {
        $limit = min((int) $request->integer('limit', 10), 50);

        $trees = Tree::query()
            ->orderByDesc('generated_at')
            ->orderByDesc('id')
            ->limit($limit)
            ->get()
            ->map(fn (Tree $tree) => [
                'id' => $tree->id,
                'title' => $tree->title,
                'orientation' => $tree->orientation,
                'max_depth' => $tree->max_depth,
                'child_probability' => $tree->child_probability,
                'seed' => $tree->seed,
                'node_count' => $tree->node_count,
                'generated_at' => $tree->generated_at?->toIso8601String(),
            ]);

        return response()->json([
            'data' => $trees,
        ]);
    }

    public function latest(TreeResponseBuilder $builder): JsonResponse
    {
        $tree = Tree::query()
            ->orderByDesc('generated_at')
            ->orderByDesc('id')
            ->firstOrFail();

        return response()->json([
            'data' => $builder->build($tree),
        ]);
    }

    public function show(Tree $tree, TreeResponseBuilder $builder): JsonResponse
    {
        return response()->json([
            'data' => $builder->build($tree),
        ]);
    }

    public function store(
        Request $request,
        TreeGenerationService $generator,
        TreeResponseBuilder $builder
    ): JsonResponse {
        $validated = $request->validate([
            'title' => ['nullable', 'string', 'max:255'],
            'orientation' => ['nullable', 'in:vertical,horizontal'],
            'max_depth' => ['nullable', 'integer', 'min:1', 'max:7'],
            'child_probability' => ['nullable', 'numeric', 'min:0.25', 'max:0.9'],
            'seed' => ['nullable', 'integer', 'min:1'],
        ]);

        $tree = $generator->generate($validated);

        return response()->json([
            'message' => 'Дерево успешно сгенерировано.',
            'data' => $builder->build($tree),
        ], 201);
    }
}
