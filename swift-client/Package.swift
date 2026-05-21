// swift-tools-version: 6.0

import PackageDescription

let package = Package(
    name: "swift-client",
    platforms: [
        .macOS(.v14),
    ],
    products: [
        .executable(
            name: "tree-client",
            targets: ["TreeCourseworkClient"]
        ),
    ],
    targets: [
        .executableTarget(
            name: "TreeCourseworkClient",
            path: "Sources"
        ),
    ]
)
