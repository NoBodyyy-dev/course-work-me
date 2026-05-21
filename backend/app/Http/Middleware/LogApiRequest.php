<?php

namespace App\Http\Middleware;

use App\Models\RequestLog;
use Closure;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;
use Symfony\Component\HttpFoundation\Response;
use Throwable;

class LogApiRequest
{
    public function handle(Request $request, Closure $next): Response
    {
        $response = $next($request);

        try {
            RequestLog::query()->create([
                'action' => $request->route()?->getName() ?? $request->path(),
                'method' => $request->method(),
                'endpoint' => '/'.$request->path(),
                'ip_address' => $request->ip(),
                'status_code' => $response->getStatusCode(),
                'tree_id' => $this->resolveTreeId($request, $response),
                'payload' => $this->sanitizePayload($request),
            ]);
        } catch (Throwable) {
            // Лог запроса не должен ломать основной сценарий приложения.
        }

        return $response;
    }

    private function sanitizePayload(Request $request): ?array
    {
        $payload = collect($request->except(['password', 'password_confirmation', '_token']))
            ->map(function (mixed $value) {
                if (is_scalar($value) || is_null($value)) {
                    return $value;
                }

                return json_encode($value, JSON_UNESCAPED_UNICODE);
            })
            ->all();

        return $payload === [] ? null : $payload;
    }

    private function resolveTreeId(Request $request, Response $response): ?int
    {
        $routeTree = $request->route('tree');

        if ($routeTree?->id) {
            return (int) $routeTree->id;
        }

        $requestTreeId = $request->integer('tree_id');

        if ($requestTreeId > 0) {
            return $requestTreeId;
        }

        if (! $response instanceof JsonResponse) {
            return null;
        }

        $payload = $response->getData(true);
        $responseTreeId = data_get($payload, 'data.id');

        return is_numeric($responseTreeId) ? (int) $responseTreeId : null;
    }
}
