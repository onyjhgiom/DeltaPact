# GitHub Release 发布指南：把 Windows 试玩包变成一键下载

游戏项目最影响 star 转化的不是 README 写得多好，而是**访客能不能一键玩到**。当前 406MB 的 Windows 包只在仓库目录里，访客要么 clone 整个 316MB 仓库，要么在文件列表里找 zip。挂成 Release 后，仓库右侧会出现 `Releases` 入口，README 也能放醒目的下载按钮。

## 一、待发布包的事实（均已校验，不要改数字）

| 项 | 值 |
|---|---|
| 文件 | `Releases/MoriEchoes_20261119_233520/MoriEchoes_Windows64_20261119_233520.zip` |
| 大小 | 425,410,776 字节（约 406 MB） |
| SHA-256 | `8a332fdc1bd4ce21d8ca4df9237146ceae443fe4537cd8b2d389061f413e1552` |
| 解压后大小 | 1,615,384,050 字节 |
| 归档文件 | 194 个（运行时 188 个） |
| CRC / 解压回读哈希 | 均通过 |
| 排除的调试文件 | 118 个（`.pdb` / `.mdb` 与 `BackUpThisFolder_ButDontShipItWithYourGame`） |
| 内含构建 | `MoriEchoes_20261029_173048_021`（README 标注的当前版本） |
| 启动方式 | 解压后运行包内 `START_GAME.cmd` |

> 该包为 Development 试玩构建，不是正式发行版。跨机性能、真人难度平衡与实体手柄未验收。

## 二、发布步骤（网页操作，约 5 分钟）

1. 打开 <https://github.com/onyjhgiom/DeltaPact/releases> → **Create a new release**。
2. **Tag version**：填 `v0.1.0-demo`（若提示 tag 不存在，选 `Create tag v0.1.0-demo on main`）。
3. **Release title**：填 `MoriEchoes v0.1.0 — Windows x64 Playable Demo`。
4. **Description**：复制本文件第三节的 Release Notes。
5. 把 zip 拖进 **Attach binaries** 区域（或点 `Select files` 选择上面那个 zip）。406MB 在 GitHub 单文件 2GB 限制内。
6. 勾选 **Set as the latest release**；如果是开发版试玩，也可以勾选 **Set as a pre-release**，避免被误认为正式版。
7. 点 **Publish release**，等上传完成（视网速可能几分钟）。

## 三、Release Notes（可直接复制）

```markdown
## MoriEchoes / DeltaPact — Windows x64 Playable Demo

An offline single-player 2D side-scrolling shooter roguelite built with Unity 6000.6.7f1 / C# / URP 2D / Input System.

### What's in this build

- 8 playable characters (DELTA, VANGUARD, WRAITH, HAVOC, KALT, VEIL, SERA, ROOK), each with Q/F/C skills and an X finisher
- 9 weapons, including a laser blade
- 10 stages: 5 side-scrolling, 3 vertical shaft climbs, 1 surface swim, 1 underwater stage
- 3-phase bosses with 30 unique skills, red charge telegraphs, mid-stage secret rooms and checkpoint retries
- Three 2D orthographic camera modes: tactical wide / third-person near / first-person weapon foreground

### Download

- File: `MoriEchoes_Windows64_20261119_233520.zip` (406 MB)
- Extract and run `START_GAME.cmd`
- Requires Windows 10/11 x64; may need the VC++ 2015–2022 runtime
- SHA-256: `8a332fdc1bd4ce21d8ca4df9237146ceae443fe4537cd8b2d389061f413e1552`
- Verification: `certutil -hashfile MoriEchoes_Windows64_20261119_233520.zip SHA256` (Windows) or `sha256sum ...` (Git Bash)

### Verification status

This round's build: 0 errors / 0 warnings; 1767 Player regression checks passed on the same final EXE. CRC and extracted-hash checks passed for this archive.

### Known limits

Development build, not a commercial release. Cross-machine performance, human difficulty tuning and physical gamepad play are not verified. See [HOW_TO_PLAY.md](https://github.com/onyjhgiom/DeltaPact/blob/main/HOW_TO_PLAY.md) for controls and details.

### License

MulanPSL-2.0. Art is development-stage generated/placeholder work, not final commercial artwork.
```

## 四、发布后回到 README 加下载按钮

发布成功后，把下面这段加到 `README.md` 和 `README_EN.md` 顶部链接区（把 `v0.1.0-demo` 换成你实际用的 tag）：

```html
<p align="center">
  <a href="https://github.com/onyjhgiom/DeltaPact/releases/download/v0.1.0-demo/MoriEchoes_Windows64_20261119_233520.zip">
    <img src="https://img.shields.io/badge/Download-Windows%20x64%20Demo-2ea44f?style=for-the-badge&logo=github" alt="Download Windows demo">
  </a>
</p>
```

> shields.io 只是按钮图标服务，不加载游戏资源；如果担心外部依赖，也可以直接放一行 Markdown 链接：
> `[下载 Windows x64 试玩包（406 MB）](https://github.com/onyjhgiom/DeltaPact/releases/download/v0.1.0-demo/MoriEchoes_Windows64_20261119_233520.zip)`。

## 五、本地重打包（可选）

如果要从当前源码重新打包，而不是复用已校验的 11-19 包：

```bash
cd C:/UnityProjects/MoriEchoes
python Tools/Package-WindowsGame.py
```

打包后务必核对新包的 SHA-256 与 `*.package.json`，再更新本文件第一节的数字和第三节的 Release Notes。不要在未校验前发布。

## 六、注意

- GitHub Release 资产单文件上限 2GB，406MB 没问题。
- 上传中断可重传同名文件；发布后不要直接覆盖资产文件，否则旧下载链接的校验值会失效——应新建 Release 或新资产。
- Release 与 Gitee 无关；Gitee 的附件需在 Gitee 仓库单独上传。
