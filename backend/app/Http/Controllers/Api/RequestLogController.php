<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\RequestLog;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;

class RequestLogController extends Controller
{
    public function index(Request $request): JsonResponse
    {
        $limit = min((int) $request->integer('limit', 25), 100);

        $logs = RequestLog::query()
            ->latest()
            ->with('tree:id,title')
            ->limit($limit)
            ->get()
            ->map(fn (RequestLog $log) => [
                'id' => $log->id,
                'action' => $log->action,
                'method' => $log->method,
                'endpoint' => $log->endpoint,
                'ip_address' => $log->ip_address,
                'status_code' => $log->status_code,
                'tree' => $log->tree ? [
                    'id' => $log->tree->id,
                    'title' => $log->tree->title,
                ] : null,
                'payload' => $log->payload,
                'created_at' => $log->created_at?->toIso8601String(),
            ]);

        return response()->json([
            'data' => $logs,
        ]);
    }
}
