# Simai Auto — maimai 自制谱一键上机工具

将 Simai 自制谱转换为 maimai 官机 opt 增量资源包。

## 首次使用

1. 解压 `simai-auto.zip`
2. **放入音频模板**: 从游戏 `StreamingAssets/Axxx/SoundData/` 随便复制一对 `.acb` + `.awb`，重命名为 `template.acb` + `template.awb`，放入 `tools/`
3. 可选: 放入 `ffmpeg.exe` 到 `tools/` (用于自动裁剪音频偏移)
4. 运行 `SimaiAuto.exe`

## 使用流程

选择文件夹或 zip → 自动解析谱面 → 显示摘要 → 输入 6 位 ID → 确认 → 全自动转换 → 输出到 `output/A500/`

## 输出

```
output/A500/
├── music/music0xxxxxx/     Music.xml + .ma2 谱面文件
├── SoundData/              .acb + .awb (自动生成)
├── LocalAssets/            封面图片 (AquaMai 读取)
└── MovieData/              BGA (可选)
```

## 6 位 ID

| 格式 | 类型 |
|------|------|
| `0xxxxx` | 标准谱面 |
| `1xxxxx` | DX 谱面 |

## 常见问题

**Q: "未找到模板 ACB"** → 从游戏 SoundData 复制任意 .acb/.awb 改名 template 放入 tools/

**Q: ACE 自动化失败** → ACE 窗口可能被其他窗口遮挡，重试即可

**Q: 谱面转换失败** → 检查 maidata.txt 格式，确保是有效的 simai 格式
