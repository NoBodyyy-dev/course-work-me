import SwiftUI

struct ContentView: View {
    @StateObject private var viewModel = AppViewModel()

    var body: some View {
        HSplitView {
            sidebar
                .frame(minWidth: 340, idealWidth: 360, maxWidth: 400)

            mainPanel
                .frame(minWidth: 720, maxWidth: .infinity, maxHeight: .infinity)
        }
        .frame(minWidth: 1160, minHeight: 820)
        .task {
            await viewModel.bootstrapIfNeeded()
        }
    }

    private var sidebar: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 18) {
                panel("Laravel API") {
                    TextField("http://127.0.0.1:8000/api", text: $viewModel.baseURL)
                        .textFieldStyle(.roundedBorder)

                    Button("Обновить данные") {
                        Task {
                            await viewModel.reloadAll(loadLatestTree: true)
                        }
                    }
                    .buttonStyle(.bordered)
                }

                panel("Генерация дерева") {
                    TextField("Название дерева", text: $viewModel.titleText)
                        .textFieldStyle(.roundedBorder)

                    Picker("Ориентация", selection: $viewModel.orientation) {
                        ForEach(TreeOrientation.allCases) { orientation in
                            Text(orientation.title).tag(orientation)
                        }
                    }
                    .pickerStyle(.segmented)

                    Stepper("Глубина: \(viewModel.maxDepth)", value: $viewModel.maxDepth, in: 1 ... 7)

                    VStack(alignment: .leading, spacing: 6) {
                        HStack {
                            Text("Вероятность дочернего узла")
                            Spacer()
                            Text(viewModel.childProbability, format: .number.precision(.fractionLength(2)))
                                .foregroundStyle(.secondary)
                        }

                        Slider(value: $viewModel.childProbability, in: 0.25 ... 0.9, step: 0.05)
                    }

                    HStack(spacing: 8) {
                        TextField("Seed", value: $viewModel.seed, format: .number)
                            .textFieldStyle(.roundedBorder)

                        Button("Случайно") {
                            viewModel.randomizeSeed()
                        }
                        .buttonStyle(.bordered)
                    }

                    HStack(spacing: 8) {
                        Button("Сгенерировать") {
                            Task {
                                await viewModel.generateTree()
                            }
                        }
                        .buttonStyle(.borderedProminent)
                        .disabled(viewModel.isLoading)

                        Button("Последнее") {
                            Task {
                                await viewModel.loadLatestTree()
                            }
                        }
                        .buttonStyle(.bordered)
                        .disabled(viewModel.isLoading)
                    }
                }

                panel("Список деревьев") {
                    if viewModel.treeHistory.isEmpty {
                        Text("Список пока пуст.")
                            .foregroundStyle(.secondary)
                    } else {
                        VStack(alignment: .leading, spacing: 8) {
                            ForEach(viewModel.treeHistory) { tree in
                                Button {
                                    Task {
                                        await viewModel.loadTree(id: tree.id)
                                    }
                                } label: {
                                    VStack(alignment: .leading, spacing: 4) {
                                        Text(tree.displayTitle)
                                            .font(.headline)
                                            .foregroundStyle(.primary)
                                        Text(tree.subtitle)
                                            .font(.caption)
                                            .foregroundStyle(.secondary)
                                    }
                                    .frame(maxWidth: .infinity, alignment: .leading)
                                    .padding(10)
                                    .background(tree.id == viewModel.selectedTreeID ? Color.accentColor.opacity(0.12) : Color(nsColor: .controlBackgroundColor))
                                    .clipShape(RoundedRectangle(cornerRadius: 8))
                                }
                                .buttonStyle(.plain)
                            }
                        }
                    }
                }

                panel("История запросов") {
                    if viewModel.requestLogs.isEmpty {
                        Text("Лог запросов пока пуст.")
                            .foregroundStyle(.secondary)
                    } else {
                        VStack(alignment: .leading, spacing: 10) {
                            ForEach(viewModel.requestLogs) { log in
                                VStack(alignment: .leading, spacing: 4) {
                                    HStack {
                                        Text("\(log.method) \(log.endpoint)")
                                            .font(.caption.bold())
                                        Spacer()
                                        Text("\(log.statusCode)")
                                            .font(.caption.monospacedDigit())
                                            .foregroundStyle(log.statusCode < 400 ? .green : .red)
                                    }

                                    Text(log.tree?.displayTitle ?? log.action)
                                        .font(.caption)
                                        .foregroundStyle(.secondary)

                                    if let createdAt = log.createdAt {
                                        Text(createdAt.formatted(date: .numeric, time: .standard))
                                            .font(.caption2)
                                            .foregroundStyle(.secondary)
                                    }
                                }
                                .padding(10)
                                .frame(maxWidth: .infinity, alignment: .leading)
                                .background(Color(nsColor: .controlBackgroundColor))
                                .clipShape(RoundedRectangle(cornerRadius: 8))
                            }
                        }
                    }
                }
            }
            .padding(18)
        }
        .background(Color(nsColor: .windowBackgroundColor))
    }

    private var mainPanel: some View {
        VStack(alignment: .leading, spacing: 16) {
            VStack(alignment: .leading, spacing: 6) {
                Text("Вариант 8: генерация и визуализация дерева")
                    .font(.largeTitle.bold())

                Text("Laravel создает структуру дерева и сохраняет вершины в PostgreSQL, Swift отображает полученный граф.")
                    .foregroundStyle(.secondary)
            }

            if let infoMessage = viewModel.infoMessage {
                statusBanner(infoMessage, color: .green.opacity(0.12), stroke: .green.opacity(0.25))
            }

            if let errorMessage = viewModel.errorMessage {
                statusBanner(errorMessage, color: .red.opacity(0.12), stroke: .red.opacity(0.25))
            }

            if let tree = viewModel.currentTree, let root = tree.root {
                VStack(alignment: .leading, spacing: 12) {
                    HStack(alignment: .top) {
                        VStack(alignment: .leading, spacing: 4) {
                            Text(tree.displayTitle)
                                .font(.title2.bold())

                            Text(tree.generatedAt?.formatted(date: .abbreviated, time: .shortened) ?? "Время генерации не указано")
                                .foregroundStyle(.secondary)
                        }

                        Spacer()

                        Picker("Вид", selection: $viewModel.renderOrientation) {
                            ForEach(TreeOrientation.allCases) { orientation in
                                Text(orientation.title).tag(orientation)
                            }
                        }
                        .pickerStyle(.segmented)
                        .frame(width: 280)
                    }

                    HStack(spacing: 12) {
                        StatBadge(title: "Узлы", value: "\(tree.nodeCount)")
                        StatBadge(title: "Глубина", value: "\(tree.maxDepth)")
                        StatBadge(title: "Вероятность", value: tree.childProbability.formatted(.number.precision(.fractionLength(2))))
                        StatBadge(title: "Seed", value: "\(tree.seed)")
                    }

                    TreeCanvasView(root: root, orientation: viewModel.renderOrientation)
                        .clipShape(RoundedRectangle(cornerRadius: 8))
                        .overlay(
                            RoundedRectangle(cornerRadius: 8)
                                .stroke(Color.accentColor.opacity(0.12), lineWidth: 1)
                        )
                }
                .frame(maxWidth: .infinity, maxHeight: .infinity, alignment: .topLeading)
            } else {
                Spacer()

                VStack(spacing: 12) {
                    Text("Нет данных для отображения")
                        .font(.title2.bold())

                    Text("Сгенерируйте дерево или загрузите последнее сохраненное дерево.")
                        .foregroundStyle(.secondary)
                }
                .frame(maxWidth: .infinity)

                Spacer()
            }
        }
        .padding(24)
        .overlay(alignment: .topTrailing) {
            if viewModel.isLoading {
                ProgressView()
                    .controlSize(.large)
                    .padding()
            }
        }
    }

    private func panel<Content: View>(_ title: String, @ViewBuilder content: () -> Content) -> some View {
        VStack(alignment: .leading, spacing: 12) {
            Text(title)
                .font(.headline)

            content()
        }
        .padding(14)
        .frame(maxWidth: .infinity, alignment: .leading)
        .background(Color(nsColor: .controlBackgroundColor))
        .clipShape(RoundedRectangle(cornerRadius: 8))
    }

    private func statusBanner(_ text: String, color: Color, stroke: Color) -> some View {
        Text(text)
            .font(.callout)
            .padding(12)
            .frame(maxWidth: .infinity, alignment: .leading)
            .background(
                RoundedRectangle(cornerRadius: 8)
                    .fill(color)
            )
            .overlay(
                RoundedRectangle(cornerRadius: 8)
                    .stroke(stroke, lineWidth: 1)
            )
    }
}

private struct StatBadge: View {
    let title: String
    let value: String

    var body: some View {
        VStack(alignment: .leading, spacing: 4) {
            Text(title)
                .font(.caption)
                .foregroundStyle(.secondary)

            Text(value)
                .font(.title3.monospacedDigit().bold())
                .foregroundStyle(.primary)
        }
        .padding(12)
        .frame(maxWidth: .infinity, alignment: .leading)
        .background(Color(nsColor: .controlBackgroundColor))
        .clipShape(RoundedRectangle(cornerRadius: 8))
    }
}
