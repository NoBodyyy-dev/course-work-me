import SwiftUI

struct ContentView: View {
    @StateObject private var viewModel = AppViewModel()

    var body: some View {
        NavigationStack {
            ZStack(alignment: .topTrailing) {
                ScrollView {
                    VStack(alignment: .leading, spacing: 18) {
                        panel("Laravel API") {
                            TextField("http://127.0.0.1:8000/api", text: $viewModel.baseURL)
                                .textInputAutocapitalization(.never)
                                .autocorrectionDisabled()
                                .keyboardType(.URL)

                            Text("Симулятор: 127.0.0.1. На iPhone: IP Mac в Wi‑Fi и php artisan serve --host=0.0.0.0")
                                .font(.caption2)
                                .foregroundStyle(.secondary)

                            Button("Обновить данные") {
                                Task {
                                    await viewModel.reloadAll(loadLatestTree: true)
                                }
                            }
                            .buttonStyle(.bordered)
                        }

                        panel("Генерация дерева") {
                            TextField("Название дерева", text: $viewModel.titleText)
                                .textInputAutocapitalization(.sentences)

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
                                    .keyboardType(.numberPad)

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
                                    Text("Нажмите строку — откроется экран с графом (вершины и направленные рёбра).")
                                        .font(.caption2)
                                        .foregroundStyle(.secondary)

                                    ForEach(viewModel.treeHistory) { tree in
                                        NavigationLink(value: tree.id) {
                                            HStack {
                                                VStack(alignment: .leading, spacing: 4) {
                                                    Text(tree.displayTitle)
                                                        .font(.headline)
                                                        .foregroundStyle(.primary)
                                                    Text(tree.subtitle)
                                                        .font(.caption)
                                                        .foregroundStyle(.secondary)
                                                }
                                                Spacer()
                                                Image(systemName: "arrow.triangle.branch")
                                                    .foregroundStyle(.secondary)
                                            }
                                            .padding(10)
                                            .frame(maxWidth: .infinity, alignment: .leading)
                                            .background(tree.id == viewModel.selectedTreeID ? Color.accentColor.opacity(0.12) : ThemeColors.cardBackground)
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
                                        .background(ThemeColors.cardBackground)
                                        .clipShape(RoundedRectangle(cornerRadius: 8))
                                    }
                                }
                            }
                        }

                        if let tree = viewModel.currentTree, let root = tree.root {
                            panel("Текущее дерево (кратко)") {
                                VStack(alignment: .leading, spacing: 10) {
                                    Text(tree.displayTitle)
                                        .font(.headline)
                                    NavigationLink("Открыть полный граф", value: tree.id)
                                        .buttonStyle(.borderedProminent)

                                    TreeCanvasView(root: root, orientation: viewModel.renderOrientation, useInternalScroll: true)
                                        .frame(minHeight: 240)
                                        .clipShape(RoundedRectangle(cornerRadius: 8))
                                }
                            }
                        }
                    }
                    .padding(18)
                }
                .background(ThemeColors.groupedBackground)
                .navigationTitle("Курсовая")
                .navigationDestination(for: Int.self) { treeId in
                    TreeGraphDetailView(viewModel: viewModel, treeId: treeId)
                }
                .task {
                    await viewModel.bootstrapIfNeeded()
                }

                if viewModel.isLoading {
                    ProgressView()
                        .padding()
                }
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
        .background(ThemeColors.cardBackground)
        .clipShape(RoundedRectangle(cornerRadius: 8))
    }
}
