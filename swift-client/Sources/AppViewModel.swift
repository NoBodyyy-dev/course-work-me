import Foundation

@MainActor
final class AppViewModel: ObservableObject {
    @Published var baseURL: String = "http://127.0.0.1:8000/api"
    @Published var titleText: String = "Сгенерированное дерево"
    @Published var orientation: TreeOrientation = .vertical
    @Published var renderOrientation: TreeOrientation = .vertical
    @Published var maxDepth: Int = 4
    @Published var childProbability: Double = 0.6
    @Published var seed: Int = 42

    @Published var currentTree: TreeRecord?
    @Published var treeHistory: [TreeSummary] = []
    @Published var requestLogs: [RequestLogEntry] = []
    @Published var selectedTreeID: Int?
    @Published var isLoading: Bool = false
    @Published var errorMessage: String?
    @Published var infoMessage: String?

    private let client = APIClient()
    private var hasLoaded = false

    func bootstrapIfNeeded() async {
        guard !hasLoaded else {
            return
        }

        hasLoaded = true
        await reloadAll(loadLatestTree: true)
    }

    func reloadAll(loadLatestTree: Bool = false) async {
        await withLoadingState {
            async let treeHistory = client.fetchTrees(baseURL: baseURL)
            async let requestLogs = client.fetchRequestLogs(baseURL: baseURL)

            self.treeHistory = try await treeHistory
            self.requestLogs = try await requestLogs

            if loadLatestTree {
                do {
                    let latestTree = try await client.fetchLatestTree(baseURL: baseURL)
                    applyLoadedTree(latestTree)
                } catch let error as APIError {
                    if case .server(statusCode: 404, _) = error {
                        currentTree = nil
                        infoMessage = "Пока нет сгенерированных деревьев."
                    } else {
                        throw error
                    }
                }
            }
        }
    }

    func generateTree() async {
        let payload = makePayload()

        await withLoadingState {
            let tree = try await client.generateTree(baseURL: baseURL, payload: payload)
            applyLoadedTree(tree)
            infoMessage = "Дерево сгенерировано и сохранено в PostgreSQL."

            async let treeHistory = client.fetchTrees(baseURL: baseURL)
            async let requestLogs = client.fetchRequestLogs(baseURL: baseURL)

            self.treeHistory = try await treeHistory
            self.requestLogs = try await requestLogs
        }
    }

    func loadLatestTree() async {
        await withLoadingState {
            do {
                let tree = try await client.fetchLatestTree(baseURL: baseURL)
                applyLoadedTree(tree)
                infoMessage = "Загружено последнее дерево."
            } catch let error as APIError {
                if case .server(statusCode: 404, _) = error {
                    currentTree = nil
                    infoMessage = "Сохраненных деревьев пока нет."
                } else {
                    throw error
                }
            }

            async let treeHistory = client.fetchTrees(baseURL: baseURL)
            async let requestLogs = client.fetchRequestLogs(baseURL: baseURL)

            self.treeHistory = try await treeHistory
            self.requestLogs = try await requestLogs
        }
    }

    func loadTree(id: Int) async {
        await withLoadingState {
            let tree = try await client.fetchTree(baseURL: baseURL, id: id)
            applyLoadedTree(tree)
            infoMessage = "Загружено дерево #\(id)."

            async let requestLogs = client.fetchRequestLogs(baseURL: baseURL)
            self.requestLogs = try await requestLogs
        }
    }

    func randomizeSeed() {
        seed = Int.random(in: 1 ... 999_999)
    }

    private func makePayload() -> GenerateTreeRequest {
        let trimmedTitle = titleText.trimmingCharacters(in: .whitespacesAndNewlines)

        return GenerateTreeRequest(
            title: trimmedTitle.isEmpty ? nil : trimmedTitle,
            orientation: orientation,
            maxDepth: maxDepth,
            childProbability: childProbability,
            seed: max(1, seed)
        )
    }

    private func applyLoadedTree(_ tree: TreeRecord) {
        currentTree = tree
        selectedTreeID = tree.id
        renderOrientation = tree.orientation
        orientation = tree.orientation
        titleText = tree.title ?? "Сгенерированное дерево"
        maxDepth = tree.maxDepth
        childProbability = tree.childProbability
        seed = tree.seed
    }

    private func withLoadingState(_ action: () async throws -> Void) async {
        isLoading = true
        errorMessage = nil

        do {
            try await action()
        } catch {
            errorMessage = error.localizedDescription
            infoMessage = nil
        }

        isLoading = false
    }
}
