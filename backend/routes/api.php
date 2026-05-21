<?php

use App\Http\Controllers\Api\RequestLogController;
use App\Http\Controllers\Api\TreeController;
use Illuminate\Support\Facades\Route;

Route::get('/trees', [TreeController::class, 'index'])->name('trees.index');
Route::get('/trees/latest', [TreeController::class, 'latest'])->name('trees.latest');
Route::get('/trees/{tree}', [TreeController::class, 'show'])->name('trees.show');
Route::post('/trees/generate', [TreeController::class, 'store'])->name('trees.generate');

Route::get('/request-logs', [RequestLogController::class, 'index'])->name('request-logs.index');
