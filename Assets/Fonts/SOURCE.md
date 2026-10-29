# Noto Sans CJK SC Regular

Official source: https://github.com/notofonts/noto-cjk/blob/main/Sans/OTF/SimplifiedChinese/NotoSansCJKsc-Regular.otf

Downloaded 2026-10-25 from the upstream `main` branch. This is the unmodified static OpenType font. Its complete copyright and SIL Open Font License 1.1 are retained in `OFL.txt` and copied into each portable distribution under `ThirdPartyNotices`.

Unity uses a dynamic font atlas with **Include Font Data** enabled. `Assets/Settings/MoriEchoesConfig.asset` references this font so UI Chinese glyphs do not depend on a Chinese Windows language pack. The import/build entry point is `MoriEchoes.EditorTools.PortableFontSetup.BuildWindows`.
