# 《三角洲契约》Codex 后续全流程执行包

这是给 Codex 在现有 Unity 6 项目中继续开发使用的任务包。它包含全流程主指令、游戏规格、美术管线、验收表和构建指令。

> 本包是开发指令与规格文件，不是已经编译验证的完整游戏。Codex 能否直接编辑 Unity Scene/Prefab、运行 Editor 或 Build，取决于当前环境权限。必须如实报告测试状态。

## 使用方式
1. 下载并解压本包。
2. 将 `CodexPrompts` 和 `Docs` 复制到 Unity 项目根目录，合并目录，不覆盖已有文件。
3. 用 Codex 打开 Unity 项目根目录。
4. 将 `CodexPrompts/MASTER_CONTINUE_ALL.md` 全文发给 Codex。
5. 允许它分阶段连续执行；如中断或需要你在 Unity Editor 操作，完成后再发 `CodexPrompts/RESUME.md`。
6. 每个里程碑保存 Git 版本或备份。不要接受未实际编译的“完成”声明。

## 重要边界
- 如果当前目录不是 Unity 项目根目录，先定位项目，不要在错误目录生成。
- 不要自动升级 Unity、删除文件或覆盖用户已有内容。
- 美术提示词不是图像资产本身；先用占位图完成玩法，再导入 PNG/音频。
- 首版只做离线单人 Windows PC MVP；不加入联网、账号、广告、内购、云存档或多人联机。
