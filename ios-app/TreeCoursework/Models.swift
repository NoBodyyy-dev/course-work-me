import Foundation

enum TreeOrientation: String, CaseIterable, Codable, Identifiable {
    case vertical
    case horizontal

    var id: String { rawValue }

    var title: String {
        switch self {
        case .vertical:
            return "Вертикально"
        case .horizontal:
            return "Горизонтально"
        }
    }
}

struct GenerateTreeRequest: Encodable {
    var title: String?
    var orientation: TreeOrientation
    var maxDepth: Int
    var childProbability: Double
    var seed: Int
}

struct APIEnvelope<T: Decodable>: Decodable {
    let data: T
}

struct TreeSummary: Decodable, Identifiable {
    let id: Int
    let title: String?
    let orientation: TreeOrientation
    let maxDepth: Int
    let childProbability: Double
    let seed: Int
    let nodeCount: Int
    let generatedAt: Date?

    var displayTitle: String {
        if let title, !title.isEmpty {
            return title
        }

        return "Дерево #\(id)"
    }

    var subtitle: String {
        let dateText = generatedAt?.formatted(date: .abbreviated, time: .shortened) ?? "без времени"
        return "\(orientation.title) • глубина \(maxDepth) • \(nodeCount) узл. • \(dateText)"
    }
}

struct TreeRecord: Decodable, Identifiable {
    let id: Int
    let title: String?
    let orientation: TreeOrientation
    let maxDepth: Int
    let childProbability: Double
    let seed: Int
    let nodeCount: Int
    let generatedAt: Date?
    let root: TreeNode?

    var displayTitle: String {
        if let title, !title.isEmpty {
            return title
        }

        return "Дерево #\(id)"
    }
}

struct TreeNode: Decodable, Identifiable, Hashable {
    let id: Int
    let number: Int
    let label: String
    let depth: Int
    let path: String
    let childSlot: Int?
    let childNumbers: [Int]
    let children: [TreeNode]
}

struct LogTreeReference: Decodable {
    let id: Int
    let title: String?

    var displayTitle: String {
        if let title, !title.isEmpty {
            return title
        }

        return "Дерево #\(id)"
    }
}

enum JSONScalar: Decodable {
    case string(String)
    case int(Int)
    case double(Double)
    case bool(Bool)
    case null

    init(from decoder: Decoder) throws {
        let container = try decoder.singleValueContainer()

        if container.decodeNil() {
            self = .null
        } else if let value = try? container.decode(Bool.self) {
            self = .bool(value)
        } else if let value = try? container.decode(Int.self) {
            self = .int(value)
        } else if let value = try? container.decode(Double.self) {
            self = .double(value)
        } else if let value = try? container.decode(String.self) {
            self = .string(value)
        } else {
            throw DecodingError.dataCorruptedError(in: container, debugDescription: "Unsupported JSON scalar.")
        }
    }

    var displayValue: String {
        switch self {
        case let .string(value):
            return value
        case let .int(value):
            return String(value)
        case let .double(value):
            return value.formatted(.number.precision(.fractionLength(0 ... 2)))
        case let .bool(value):
            return value ? "true" : "false"
        case .null:
            return "null"
        }
    }
}

struct RequestLogEntry: Decodable, Identifiable {
    let id: Int
    let action: String
    let method: String
    let endpoint: String
    let ipAddress: String?
    let statusCode: Int
    let tree: LogTreeReference?
    let payload: [String: JSONScalar]?
    let createdAt: Date?
}
