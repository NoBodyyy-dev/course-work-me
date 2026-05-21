import Foundation

enum APIError: LocalizedError {
    case invalidBaseURL
    case invalidResponse
    case server(statusCode: Int, message: String)

    var errorDescription: String? {
        switch self {
        case .invalidBaseURL:
            return "Некорректный адрес Laravel API."
        case .invalidResponse:
            return "Сервер вернул неожиданный ответ."
        case let .server(statusCode, message):
            return "Ошибка \(statusCode): \(message)"
        }
    }
}

struct APIClient {
    private let session: URLSession = .shared

    func fetchLatestTree(baseURL: String) async throws -> TreeRecord {
        try await request(baseURL: baseURL, path: "trees/latest")
    }

    func fetchTrees(baseURL: String, limit: Int = 15) async throws -> [TreeSummary] {
        try await request(baseURL: baseURL, path: "trees?limit=\(limit)")
    }

    func fetchTree(baseURL: String, id: Int) async throws -> TreeRecord {
        try await request(baseURL: baseURL, path: "trees/\(id)")
    }

    func fetchRequestLogs(baseURL: String, limit: Int = 15) async throws -> [RequestLogEntry] {
        try await request(baseURL: baseURL, path: "request-logs?limit=\(limit)")
    }

    func generateTree(baseURL: String, payload: GenerateTreeRequest) async throws -> TreeRecord {
        let body = try makeEncoder().encode(payload)
        return try await request(
            baseURL: baseURL,
            path: "trees/generate",
            method: "POST",
            body: body
        )
    }

    private func request<T: Decodable>(
        baseURL: String,
        path: String,
        method: String = "GET",
        body: Data? = nil
    ) async throws -> T {
        let url = try makeURL(baseURL: baseURL, path: path)
        var request = URLRequest(url: url)
        request.httpMethod = method
        request.setValue("application/json", forHTTPHeaderField: "Accept")

        if let body {
            request.httpBody = body
            request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        }

        let (data, response) = try await session.data(for: request)

        guard let httpResponse = response as? HTTPURLResponse else {
            throw APIError.invalidResponse
        }

        guard (200 ... 299).contains(httpResponse.statusCode) else {
            throw APIError.server(
                statusCode: httpResponse.statusCode,
                message: extractErrorMessage(from: data)
            )
        }

        let envelope = try makeDecoder().decode(APIEnvelope<T>.self, from: data)
        return envelope.data
    }

    private func makeURL(baseURL: String, path: String) throws -> URL {
        let trimmedBase = baseURL.trimmingCharacters(in: .whitespacesAndNewlines)
            .trimmingCharacters(in: CharacterSet(charactersIn: "/"))

        guard let url = URL(string: "\(trimmedBase)/\(path)") else {
            throw APIError.invalidBaseURL
        }

        return url
    }

    private func makeDecoder() -> JSONDecoder {
        let decoder = JSONDecoder()
        decoder.keyDecodingStrategy = .convertFromSnakeCase
        decoder.dateDecodingStrategy = .iso8601
        return decoder
    }

    private func makeEncoder() -> JSONEncoder {
        let encoder = JSONEncoder()
        encoder.keyEncodingStrategy = .convertToSnakeCase
        return encoder
    }

    private func extractErrorMessage(from data: Data) -> String {
        if let object = try? JSONSerialization.jsonObject(with: data) as? [String: Any] {
            if let message = object["message"] as? String, !message.isEmpty {
                return message
            }

            if let errors = object["errors"] as? [String: [String]] {
                return errors.values.flatMap { $0 }.joined(separator: "\n")
            }
        }

        return "Не удалось выполнить запрос."
    }
}
