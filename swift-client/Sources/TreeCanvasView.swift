import SwiftUI

struct TreeCanvasView: View {
    let root: TreeNode
    let orientation: TreeOrientation

    private let nodeSize = CGSize(width: 104, height: 72)
    private let levelSpacing: CGFloat = 88
    private let siblingSpacing: CGFloat = 30
    private let padding: CGFloat = 40

    var body: some View {
        let layout = TreeLayout(
            root: root,
            orientation: orientation,
            nodeSize: nodeSize,
            levelSpacing: levelSpacing,
            siblingSpacing: siblingSpacing,
            padding: padding
        )

        ScrollView([.horizontal, .vertical]) {
            ZStack(alignment: .topLeading) {
                ForEach(layout.edges, id: \.self) { edge in
                    Path { path in
                        let parentPoint = layout.point(for: edge.parentID)
                        let childPoint = layout.point(for: edge.childID)
                        path.move(to: parentPoint)
                        path.addLine(to: childPoint)
                    }
                    .stroke(Color.accentColor.opacity(0.35), lineWidth: 2)
                }

                ForEach(layout.nodes) { entry in
                    TreeNodeBadge(node: entry.node)
                        .position(entry.position)
                }
            }
            .frame(width: layout.canvasSize.width, height: layout.canvasSize.height, alignment: .topLeading)
            .padding(8)
        }
        .background(Color(nsColor: .textBackgroundColor))
    }
}

private struct TreeNodeBadge: View {
    let node: TreeNode

    var body: some View {
        VStack(spacing: 4) {
            Text(node.label)
                .font(.headline.monospacedDigit())
                .foregroundStyle(.primary)

            Text(node.childNumbersText)
                .font(.caption.monospacedDigit())
                .foregroundStyle(.secondary)
                .lineLimit(1)

            Text("Уровень \(node.depth)")
                .font(.caption2)
                .foregroundStyle(.secondary)
        }
        .frame(width: 104, height: 72)
        .background(
            RoundedRectangle(cornerRadius: 8)
                .fill(Color(nsColor: .controlBackgroundColor))
        )
        .overlay(
            RoundedRectangle(cornerRadius: 8)
                .stroke(Color.accentColor.opacity(0.25), lineWidth: 1)
        )
        .shadow(color: .black.opacity(0.06), radius: 4, y: 2)
    }
}

private struct TreeLayout {
    struct PositionedNode: Identifiable {
        let node: TreeNode
        let position: CGPoint

        var id: Int { node.id }
    }

    struct Edge: Hashable {
        let parentID: Int
        let childID: Int
    }

    let nodes: [PositionedNode]
    let edges: [Edge]
    let canvasSize: CGSize

    private let pointsByID: [Int: CGPoint]

    init(
        root: TreeNode,
        orientation: TreeOrientation,
        nodeSize: CGSize,
        levelSpacing: CGFloat,
        siblingSpacing: CGFloat,
        padding: CGFloat
    ) {
        var rawPositions: [Int: CGPoint] = [:]
        var rawNodes: [TreeNode] = []
        var edges: [Edge] = []
        var nextLeafX: CGFloat = 0
        var maxDepth = 0

        func visit(_ node: TreeNode, depth: Int) -> CGFloat {
            rawNodes.append(node)
            maxDepth = max(maxDepth, depth)

            if node.children.isEmpty {
                let leafX = nextLeafX
                nextLeafX += 1
                rawPositions[node.id] = CGPoint(x: leafX, y: CGFloat(depth))
                return leafX
            }

            let childXs = node.children.map { child -> CGFloat in
                edges.append(Edge(parentID: node.id, childID: child.id))
                return visit(child, depth: depth + 1)
            }

            let currentX = (childXs.first! + childXs.last!) / 2
            rawPositions[node.id] = CGPoint(x: currentX, y: CGFloat(depth))
            return currentX
        }

        _ = visit(root, depth: 0)

        let horizontalStep = nodeSize.width + siblingSpacing
        let verticalStep = nodeSize.height + levelSpacing
        let maxBreadth = rawPositions.values.map(\.x).max() ?? 0

        func orient(_ rawPoint: CGPoint) -> CGPoint {
            let x = padding + (rawPoint.x * horizontalStep) + (nodeSize.width / 2)
            let y = padding + (rawPoint.y * verticalStep) + (nodeSize.height / 2)

            switch orientation {
            case .vertical:
                return CGPoint(x: x, y: y)
            case .horizontal:
                return CGPoint(x: y, y: x)
            }
        }

        let pointsByID = rawPositions.mapValues(orient)
        self.pointsByID = pointsByID
        self.edges = edges
        self.nodes = rawNodes.compactMap { node in
            guard let point = pointsByID[node.id] else {
                return nil
            }

            return PositionedNode(node: node, position: point)
        }

        let verticalCanvas = CGSize(
            width: padding * 2 + ((maxBreadth + 1) * horizontalStep) + nodeSize.width,
            height: padding * 2 + (CGFloat(maxDepth) * verticalStep) + nodeSize.height
        )

        switch orientation {
        case .vertical:
            canvasSize = verticalCanvas
        case .horizontal:
            canvasSize = CGSize(width: verticalCanvas.height, height: verticalCanvas.width)
        }
    }

    func point(for id: Int) -> CGPoint {
        pointsByID[id] ?? .zero
    }
}

private extension TreeNode {
    var childNumbersText: String {
        if childNumbers.isEmpty {
            return "Дочерние: нет"
        }

        let values = childNumbers.map(String.init).joined(separator: ", ")
        return "Дочерние: \(values)"
    }
}
