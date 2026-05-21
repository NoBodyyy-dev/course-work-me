import SwiftUI

/// Экран полной визуализации дерева как ориентированного графа после выбора из списка.
struct TreeGraphDetailView: View {
    @ObservedObject var viewModel: AppViewModel
    let treeId: Int

    var body: some View {
        Group {
            if let tree = viewModel.currentTree, tree.id == treeId, let root = tree.root {
                ScrollView([.horizontal, .vertical]) {
                    VStack(alignment: .leading, spacing: 16) {
                        header(tree: tree)

                        Picker("Вид", selection: $viewModel.renderOrientation) {
                            ForEach(TreeOrientation.allCases) { orientation in
                                Text(orientation.title).tag(orientation)
                            }
                        }
                        .pickerStyle(.segmented)

                        LazyVGrid(columns: [GridItem(.flexible()), GridItem(.flexible())], spacing: 12) {
                            StatBadge(title: "Узлы", value: "\(tree.nodeCount)")
                            StatBadge(title: "Глубина", value: "\(tree.maxDepth)")
                            StatBadge(title: "Вероятность", value: tree.childProbability.formatted(.number.precision(.fractionLength(2))))
                            StatBadge(title: "Seed", value: "\(tree.seed)")
                        }

                        Text("Граф: вершины — узлы дерева, стрелки — рёбра от родителя к ребёнку.")
                            .font(.caption)
                            .foregroundStyle(.secondary)

                        TreeCanvasView(root: root, orientation: viewModel.renderOrientation, useInternalScroll: false)
                            .id("\(treeId)-\(viewModel.renderOrientation)")
                    }
                    .padding(16)
                }
                .background(ThemeColors.groupedBackground)
            } else if viewModel.isLoading {
                ProgressView("Загрузка дерева…")
                    .frame(maxWidth: .infinity, maxHeight: .infinity)
            } else {
                ContentUnavailableView(
                    "Нет данных",
                    systemImage: "exclamationmark.triangle",
                    description: Text("Не удалось загрузить дерево #\(treeId).")
                )
            }
        }
        .navigationTitle("Граф дерева")
        .navigationBarTitleDisplayMode(.inline)
        .task(id: treeId) {
            await viewModel.loadTree(id: treeId)
        }
    }

    private func header(tree: TreeRecord) -> some View {
        VStack(alignment: .leading, spacing: 6) {
            Text(tree.displayTitle)
                .font(.title2.bold())

            Text(tree.generatedAt?.formatted(date: .abbreviated, time: .shortened) ?? "Время не указано")
                .foregroundStyle(.secondary)
        }
        .frame(maxWidth: .infinity, alignment: .leading)
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
        .background(ThemeColors.cardBackground)
        .clipShape(RoundedRectangle(cornerRadius: 8))
    }
}
