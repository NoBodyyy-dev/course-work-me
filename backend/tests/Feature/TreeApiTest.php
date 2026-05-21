<?php

namespace Tests\Feature;

use App\Models\RequestLog;
use App\Models\Tree;
use Illuminate\Foundation\Testing\RefreshDatabase;
use Tests\TestCase;

class TreeApiTest extends TestCase
{
    use RefreshDatabase;

    public function test_it_generates_tree_and_returns_nested_structure(): void
    {
        $response = $this->postJson('/api/trees/generate', [
            'title' => 'Тестовое дерево',
            'orientation' => 'horizontal',
            'max_depth' => 3,
            'child_probability' => 0.65,
            'seed' => 42,
        ]);

        $response
            ->assertCreated()
            ->assertJsonPath('data.title', 'Тестовое дерево')
            ->assertJsonPath('data.orientation', 'horizontal')
            ->assertJsonPath('data.max_depth', 3)
            ->assertJsonPath('data.root.number', 1);

        $this->assertDatabaseHas('trees', [
            'title' => 'Тестовое дерево',
            'orientation' => 'horizontal',
        ]);

        $this->assertDatabaseCount('request_logs', 1);
        $rootChildNumbers = $response->json('data.root.child_numbers');

        $this->assertIsArray($rootChildNumbers);
        $this->assertNotEmpty($rootChildNumbers);

        $this->assertDatabaseHas('tree_nodes', [
            'tree_id' => $response->json('data.id'),
            'node_number' => 1,
            'child_numbers' => json_encode($rootChildNumbers),
        ]);

        $this->assertDatabaseHas('request_logs', [
            'action' => 'trees.generate',
            'tree_id' => $response->json('data.id'),
        ]);
    }

    public function test_it_returns_latest_tree_and_request_history(): void
    {
        $this->postJson('/api/trees/generate', [
            'title' => 'Первое дерево',
            'seed' => 7,
        ])->assertCreated();

        $this->postJson('/api/trees/generate', [
            'title' => 'Последнее дерево',
            'seed' => 9,
        ])->assertCreated();

        $latest = $this->getJson('/api/trees/latest');

        $latest
            ->assertOk()
            ->assertJsonPath('data.title', 'Последнее дерево');

        $logs = $this->getJson('/api/request-logs');

        $logs
            ->assertOk()
            ->assertJsonCount(3, 'data');

        $this->assertSame(4, RequestLog::query()->count());
    }

    public function test_every_node_stores_its_child_numbers_in_database(): void
    {
        $treeId = $this->postJson('/api/trees/generate', [
            'title' => 'Проверка дочерних узлов',
            'max_depth' => 4,
            'child_probability' => 0.7,
            'seed' => 11,
        ])->assertCreated()->json('data.id');

        $tree = Tree::query()
            ->with('nodes.children')
            ->findOrFail($treeId);

        foreach ($tree->nodes as $node) {
            $expectedChildNumbers = $node->children
                ->sortBy('child_slot')
                ->pluck('node_number')
                ->values()
                ->all();

            $this->assertSame($expectedChildNumbers, $node->child_numbers ?? []);
        }
    }
}
