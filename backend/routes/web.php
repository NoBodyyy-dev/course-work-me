<?php

use Illuminate\Support\Facades\Route;

Route::get('/', function () {
    return response()->json([
        'project' => 'Курсовой проект: генерация и визуализация дерева',
        'stack' => ['Laravel', 'PostgreSQL', 'SwiftUI'],
        'endpoints' => [
            'GET /api/trees',
            'GET /api/trees/latest',
            'GET /api/trees/{id}',
            'POST /api/trees/generate',
            'GET /api/request-logs',
        ],
    ]);
});
