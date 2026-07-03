using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HtmlRendererCore.PdfSharp.Utilities;

using global::PdfSharp.Fonts;

using Microsoft.Extensions.Logging;

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

/// <summary>
/// PdfSharp 嵌入字体解析器：
/// 1. Segoe UI / Arial 从嵌入资源加载（跨平台，Linux 不依赖 fontconfig）
/// 2. 其他字体回退给 PlatformFontResolver（Windows 走系统，Linux 6.0.0 走 libgdiplus+fontconfig）
/// </summary>
public class EmbeddedFontResolver : IFontResolver
{
    private readonly Assembly _assembly;
    private readonly ILogger _logger;

    private Dictionary<string, string> _fontPathDic = new Dictionary<string, string>();

    public bool IsLinux { get; private set; }

    public EmbeddedFontResolver(ILogger<EmbeddedFontResolver> logger = null)
    {
        _logger = logger;
        var assembly = Assembly.GetExecutingAssembly();
        _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
        IsLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    }

    public string DefaultFontName { get; set; } ="Segoe UI";

    public void RegistFontAndPath(string fontFmailyName, string fontPath)
    {
        if (!File.Exists(fontPath))
        {
            throw new FileNotFoundException($"[EmbeddedFontResolver] Embedded font not found: faceName='{fontFmailyName}', path='{fontPath}'.  Check Initialize Dictionary Parameters.");
        }
        _fontPathDic[fontFmailyName] = fontPath;
    }

    public void RegisterSystemFonts()
    {
        if (IsLinux)
        {
            // Linux：DejaVu Sans 替代 Segoe UI / Arial 
            var dejavuRegular = "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf";
            var dejavuBold = "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf";

            if (File.Exists(dejavuRegular))
            {
                _fontPathDic["dejavusans.ttf"] = dejavuRegular;
                _fontPathDic["segoeui.ttf"] = dejavuRegular;
                _fontPathDic["segoeuii.ttf"] = dejavuRegular;
                _fontPathDic["arial.ttf"] = dejavuRegular;
                _fontPathDic["ariali.ttf"] = dejavuRegular;
                _logger?.LogDebug("Registered Linux system font: DejaVuSans.ttf → Segoe UI, Arial");
            }
            if (File.Exists(dejavuBold))
            {
                _fontPathDic["segoeuib.ttf"] = dejavuBold;
                _fontPathDic["arialbd.ttf"] = dejavuBold;
                _fontPathDic["arialbi.ttf"] = dejavuBold;
                _logger?.LogDebug("Registered Linux system font: DejaVuSans-Bold.ttf → Segoe UI, Arial");
            }
        }
        else
        {
            // Windows：系统自带 Segoe UI / Arial
            var segoeUi = @"C:\Windows\Fonts\segoeui.ttf";
            var segoeUiBold = @"C:\Windows\Fonts\segoeuib.ttf";
            var segoeUiItalic = @"C:\Windows\Fonts\segoeuii.ttf";
            var arial = @"C:\Windows\Fonts\arial.ttf";
            var arialBold = @"C:\Windows\Fonts\arialbd.ttf";
            var arialItalic = @"C:\Windows\Fonts\ariali.ttf";

            if (File.Exists(segoeUi)) _fontPathDic["segoeui.ttf"] = segoeUi;
            if (File.Exists(segoeUiBold)) _fontPathDic["segoeuib.ttf"] = segoeUiBold;
            if (File.Exists(segoeUiItalic)) _fontPathDic["segoeuii.ttf"] = segoeUiItalic;
            if (File.Exists(arial)) _fontPathDic["arial.ttf"] = arial;
            if (File.Exists(arialBold)) _fontPathDic["arialbd.ttf"] = arialBold;
            if (File.Exists(arialItalic)) _fontPathDic["ariali.ttf"] = arialItalic;
            _logger?.LogDebug("Registered window system font: segoeui.ttf → Segoe UI, arial.ttf → Arial");
        }
    }

    public string GetMapFileName(string faceName) => faceName.ToLower() switch
    {
        "dejavusans" => "dejavusans.ttf",
        "segoeui" => "segoeui.ttf",
        "segoe ui" => "segoeui.ttf",
        "segoe ui bold" => "segoeuib.ttf",
        "segoe ui italic" => "segoeuii.ttf",
        "arial" => "arial.ttf",
        "arial bold" => "arialbd.ttf",
        "arial italic" => "ariali.ttf",
        _ => faceName + ".ttf"
    };

    /// <summary>
    /// PdfSharp 在找不到字体时会调用此方法，询问："你要用什么字体文件来替代？"
    /// </summary>
    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        var normalizedFamilyName = familyName?.Trim().ToLowerInvariant() ?? string.Empty;

        // 优先根据注册的字体路径字典来查找
        var fontFileName = GetMapFileName(normalizedFamilyName);
        var font = _fontPathDic.FirstOrDefault(x => x.Key.Contains(fontFileName, StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrEmpty(font.Value))
        {
            font = _fontPathDic.FirstOrDefault(x => x.Key.ToLower() == normalizedFamilyName + ".ttf");
        }
        if (!string.IsNullOrEmpty(font.Value))
        {
            return new FontResolverInfo(font.Key, isBold, isItalic);
        }

        // 拦截 Arial
        //if (normalizedFamilyName == "arial")
        //{
        //    if (isBold && isItalic) return new FontResolverInfo("arialbi.ttf", isBold, isItalic);
        //    if (isBold) return new FontResolverInfo("arialbd.ttf", isBold, isItalic);
        //    if (isItalic) return new FontResolverInfo("ariali.ttf", false, isItalic);
        //    return new FontResolverInfo("arial.ttf", isBold, isItalic);
        //}

        //2.拦截对 Segoe UI 的请求, 作为默认字体，内嵌保底，因为 linux 中没有 Segoe UI 字体，PdfSharp 6.0.0 在 linux 下会走 libgdiplus+fontconfig 找不到字体报错
        if (normalizedFamilyName.Contains("segoe") || (IsLinux && normalizedFamilyName.Contains("arial")))
        {
            if (isBold && isItalic) return new FontResolverInfo("segoeuiz.ttf", isBold, isItalic);
            if (isBold) return new FontResolverInfo("segoeuib.ttf", isBold, isItalic);
            if (isItalic) return new FontResolverInfo("segoeuii.ttf", false, isItalic);
            return new FontResolverInfo("segoeui.ttf", isBold, isItalic);
        }

        // 3. 其他字体回退
        return PlatformFontResolver.ResolveTypeface(familyName, isBold, isItalic);
    }

    public byte[] GetFont(string faceName)
    {
        // 优先从 _fontPathDic 里找，如果有就直接读取文件
        if (_fontPathDic.TryGetValue(faceName, out string facePath))
        {
            if (File.Exists(facePath))
            {
                return File.ReadAllBytes(facePath);
            }
            else if (!faceName.Contains("segoe", StringComparison.OrdinalIgnoreCase))
            {
                throw new FileNotFoundException($"[EmbeddedFontResolver] Embedded font not found: faceName='{faceName}', path='{facePath}'.  Check RegistFontAndPath is correct.");
            }
        }

        var resourceName = $"fonts.{faceName}";
        using var stream = _assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            // 调试阶段可以 throw 方便定位，上线后可 return null 让 PdfSharp 继续兜底
            throw new FileNotFoundException(
                $"[EmbeddedFontResolver] Embedded font not found: faceName='{faceName}', resource='{resourceName}'. " +
                $"Check .csproj EmbeddedResource include and project default namespace.");
        }

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}
