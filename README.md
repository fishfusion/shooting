# Unity 手机 WebGL 弹力球枪小游戏（原型）

## 已实现玩法
- 底部固定球枪。
- 手指按住屏幕并向发射方向反向拖拽，控制瞄准方向和力度，松手发射。
- 使用 `Rigidbody2D` 重力与碰撞，球落地后弹起并逐步损耗能量。
- 远处 6 个圆盘目标，大小/高度/速度都不同，仅做左右+上下周期运动（`Z` 不变）。

## 脚本文件
- `Assets/Scripts/ShooterController.cs`
- `Assets/Scripts/BallBehaviour.cs`
- `Assets/Scripts/MovingTarget.cs`
- `Assets/Scripts/TargetSpawner.cs`
- `Assets/Scripts/GameBootstrap.cs`

## Unity 场景搭建（2D）
1. 新建 `Main Camera`（Orthographic）。
2. 创建 `Shooter`（空物体）放在底部，子物体 `MuzzlePoint` 作为出球点。
3. 给 `Shooter` 挂 `ShooterController`：
   - `Main Camera` 绑定主相机
   - `Ball Prefab` 绑定球预制体
   - `Muzzle Point` 绑定 `MuzzlePoint`
   - 可选绑定 `LineRenderer` 做瞄准线
4. 创建球预制体 `Ball`：
   - `SpriteRenderer`（圆形贴图）
   - `CircleCollider2D`
   - `Rigidbody2D`（`Gravity Scale` 建议 `1.6`）
   - 挂 `BallBehaviour`
5. 创建地面 `Ground`：
   - `BoxCollider2D`
   - 使用 `PhysicsMaterial2D`（示例：`Bounciness=0.65`，`Friction=0.35`）
6. 球体的 `CircleCollider2D` 也绑定材质（示例：`Bounciness=0.78`，`Friction=0.2`），即可产生回弹和能量损耗。
7. 创建目标预制体 `TargetPrefab`：
   - `SpriteRenderer`（圆盘贴图）
   - `CircleCollider2D`（可选设为 `Is Trigger`）
   - 挂 `MovingTarget`
8. 创建 `TargetsRoot` 空物体并挂 `TargetSpawner`，把 `TargetPrefab` 拖到 `Target Prefab`。
9. 创建 `GameBootstrap` 空物体并挂 `GameBootstrap`：
   - `Shooter` 绑定射手对象
   - `TargetSpawner` 绑定 `TargetsRoot`
   - `Shooter Transform` 绑定 `Shooter`（自动贴近屏幕底部）

## WebGL 与手机建议
1. `File > Build Settings > WebGL`，`Player Settings` 中启用移动端优化（压缩建议 `Gzip/Brotli`）。
2. 画面方向建议锁定 `Portrait`（竖屏）。
3. WebGL 触摸已由 `ShooterController` 处理；PC 端可用鼠标调试。

## 可调参数（建议）
- 发射手感：
  - `maxDragDistance`：2.5~4.0
  - `forceScale`：6~10
- 弹跳衰减：
  - 主要通过 `PhysicsMaterial2D` 的 `Bounciness/Friction` 调节
- 目标运动：
  - `TargetSpawner` 里的 `_speedFactors`、`_scales` 可直接改出不同难度

## 新增：远端布料（XPBD + Ward）
1. 场景里创建空物体 `BackdropCloth`，挂上 `XPBDCloth`（会自动生成网格）。
2. 在 `GameBootstrap` 里绑定：
   - `Ground Collider`：地板的 `Collider2D`
   - `Backdrop Cloth`：`BackdropCloth`
3. 运行后会自动把布宽设为地板宽，布高设为宽的 2 倍，并挂在远端上沿位置。
4. `XPBDCloth` 默认启用与 `CircleCollider2D` 碰撞（用于球），未启用自碰撞。
5. 通过 `BackdropCloth` 的 `MeshRenderer` 使用 `Custom/WardCloth`，默认深蓝色布面和 Ward 高光。
