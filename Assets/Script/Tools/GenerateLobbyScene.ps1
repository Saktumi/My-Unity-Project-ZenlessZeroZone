$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$assets = Resolve-Path (Join-Path $scriptDir '..\..')
$lobbyScene = Join-Path $assets 'Scenes\Lobby.unity'

$script:idCounter = 50000000
function Next-Id { $script:idCounter++; return $script:idCounter }

$script:goData = @{}
$script:rtData = @{}
$script:blocks = New-Object System.Collections.ArrayList
$script:bindings = New-Object System.Collections.ArrayList
$script:compByType = @{}

function Add-Raw([string]$raw) { [void]$script:blocks.Add($raw) }

$T_GO = @'
--- !u!1 &@@GO@@
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:@@COMPS@@
  m_Layer: 5
  m_Name: @@NAME@@
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: @@ACTIVE@@
'@

$T_RT = @'
--- !u!224 &@@RT@@
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: @@GO@@}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children:@@CHILDREN@@
  m_Father: {fileID: @@FATHER@@}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 1}
  m_AnchorMax: {x: 0, y: 1}
  m_AnchoredPosition: {x: @@X@@, y: @@Y@@}
  m_SizeDelta: {x: @@W@@, y: @@H@@}
  m_Pivot: {x: 0, y: 1}
'@

$T_CR = @'
--- !u!222 &@@CR@@
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: @@GO@@}
  m_CullTransparentMesh: 1
'@

$T_IMG = @'
--- !u!114 &@@IMG@@
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: @@GO@@}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: fe87c0e1cc204ed48ad3b37840f39efc, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  m_Material: {fileID: 0}
  m_Color: @@COLOR@@
  m_RaycastTarget: @@RAY@@
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {fileID: 0}
  m_Type: 0
  m_PreserveAspect: 0
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1
'@

$T_RAWIMG = @'
--- !u!114 &@@RI@@
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: @@GO@@}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 1344c3c82d62a2a41a3576d8abb8e3ea, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.RawImage
  m_Material: {fileID: 0}
  m_Color: {r: 1, g: 1, b: 1, a: 1}
  m_RaycastTarget: 0
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Texture: {fileID: 2800000, guid: @@TEXGUID@@, type: 3}
  m_UVRect:
    serializedVersion: 2
    x: 0
    y: 0
    width: 1
    height: 1
'@

$T_SPRITEIMG = @'
--- !u!114 &@@SI@@
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: @@GO@@}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: fe87c0e1cc204ed48ad3b37840f39efc, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  m_Material: {fileID: 0}
  m_Color: {r: 1, g: 1, b: 1, a: 1}
  m_RaycastTarget: 0
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {fileID: 21300000, guid: @@SPRITEGUID@@, type: 3}
  m_Type: 0
  m_PreserveAspect: 0
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1
'@

function Add-SpriteImage([hashtable]$ui, [string]$spriteGuid) {
    $si = Next-Id
    Add-Graphic $ui $si
    Add-Raw ($T_SPRITEIMG.Replace('@@SI@@', "$si").Replace('@@GO@@', "$($ui.go)").Replace('@@SPRITEGUID@@', $spriteGuid))
    return $si
}

function Add-RawImage([hashtable]$ui, [string]$texGuid) {
    $ri = Next-Id
    Add-Graphic $ui $ri
    Add-Raw ($T_RAWIMG.Replace('@@RI@@', "$ri").Replace('@@GO@@', "$($ui.go)").Replace('@@TEXGUID@@', $texGuid))
    return $ri
}

$T_TXT = @'
--- !u!114 &@@TX@@
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: @@GO@@}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 5f7201a12d95ffc409449d95f23cf332, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  m_Material: {fileID: 0}
  m_Color: @@COLOR@@
  m_RaycastTarget: 0
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_FontData:
    m_Font: {fileID: 12800000, guid: 6ef9c88c2621fd24f9d16ec5d846dee7, type: 3}
    m_FontSize: @@SIZE@@
    m_FontStyle: 0
    m_BestFit: 0
    m_MinSize: 2
    m_MaxSize: 300
    m_Alignment: @@ALIGN@@
    m_AlignByGeometry: 0
    m_RichText: 1
    m_HorizontalOverflow: @@HO@@
    m_VerticalOverflow: @@VO@@
    m_LineSpacing: 1
  m_Text: @@TEXT@@
'@

function New-UI([string]$name, $fatherRt, [double]$x, [double]$y, [double]$w, [double]$h, [bool]$active = $true) {
    $go = Next-Id
    $rt = Next-Id
    $script:goData[$go] = @{ name = $name; active = $active; comps = New-Object System.Collections.ArrayList }
    $script:goData[$go].comps.Add($rt) | Out-Null
    $script:rtData[$rt] = @{ go = $go; name = $name; x = $x; y = $y; w = $w; h = $h; father = $(if ($fatherRt) { $fatherRt } else { 0 }); children = New-Object System.Collections.ArrayList }
    if ($fatherRt -and $script:rtData.ContainsKey($fatherRt)) { $script:rtData[$fatherRt].children.Add($rt) | Out-Null }
    return @{ go = $go; rt = $rt }
}

function Add-Comp([hashtable]$ui, [int]$compId) { $script:goData[$ui.go].comps.Add($compId) | Out-Null }

function Format-Color([string]$c) {
    $parts = $c -split ','
    if ($parts.Count -ne 4) { return "{r: 1, g: 1, b: 1, a: 1}" }
    return "{r: $($parts[0].Trim()), g: $($parts[1].Trim()), b: $($parts[2].Trim()), a: $($parts[3].Trim())}"
}

function Add-Graphic([hashtable]$ui, [int]$compId) {
    $cr = Next-Id
    Add-Comp $ui $cr
    Add-Comp $ui $compId
    Add-Raw ($T_CR.Replace('@@CR@@', "$cr").Replace('@@GO@@', "$($ui.go)"))
}

function Add-Image([hashtable]$ui, [string]$color, [int]$ray = 1) {
    $img = Next-Id
    Add-Graphic $ui $img
    Add-Raw ($T_IMG.Replace('@@IMG@@', "$img").Replace('@@GO@@', "$($ui.go)").Replace('@@COLOR@@', (Format-Color $color)).Replace('@@RAY@@', "$ray"))
    return $img
}

# 传统 Text 组件（动态萝莉体字体，支持中文渲染）；text 参数里的反斜杠会被 YAML 保留
function Add-Text([hashtable]$ui, [string]$text, [double]$size, [string]$color, [int]$align = 0, [int]$ho = 0, [int]$vo = 0) {
    $pw = $script:rtData[$ui.rt].w
    $ph = $script:rtData[$ui.rt].h
    $go = Next-Id
    $rt = Next-Id
    $script:goData[$go] = @{ name = 'Text'; active = $true; comps = New-Object System.Collections.ArrayList }
    $script:goData[$go].comps.Add($rt) | Out-Null
    $script:rtData[$rt] = @{ go = $go; name = 'Text'; x = 0; y = 0; w = $pw; h = $ph; father = $ui.rt; children = New-Object System.Collections.ArrayList }
    $script:rtData[$ui.rt].children.Add($rt) | Out-Null
    $cr = Next-Id
    $script:goData[$go].comps.Add($cr) | Out-Null
    Add-Raw ($T_CR.Replace('@@CR@@', "$cr").Replace('@@GO@@', "$go"))
    $tx = Next-Id
    $script:goData[$go].comps.Add($tx) | Out-Null
    Add-Raw ($T_TXT.Replace('@@TX@@', "$tx").Replace('@@GO@@', "$go").Replace('@@TEXT@@', $text).Replace('@@SIZE@@', "$size").Replace('@@COLOR@@', (Format-Color $color)).Replace('@@ALIGN@@', "$align").Replace('@@HO@@', "$ho").Replace('@@VO@@', "$vo"))
    return $tx
}

$T_BTN = @'
--- !u!114 &@@BTN@@
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: @@GO@@}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 4e29b1a8efbd4b44bb3f3716e73f07ff, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  m_Navigation:
    m_Mode: 3
    m_WrapAround: 0
    m_SelectOnUp: {fileID: 0}
    m_SelectOnDown: {fileID: 0}
    m_SelectOnLeft: {fileID: 0}
    m_SelectOnRight: {fileID: 0}
  m_Transition: 1
  m_Colors:
    m_NormalColor: {r: 1, g: 1, b: 1, a: 1}
    m_HighlightedColor: {r: 0.9607843, g: 0.9607843, b: 0.9607843, a: 1}
    m_PressedColor: {r: 0.78431374, g: 0.78431374, b: 0.78431374, a: 1}
    m_SelectedColor: {r: 0.9607843, g: 0.9607843, b: 0.9607843, a: 1}
    m_DisabledColor: {r: 0.78431374, g: 0.78431374, b: 0.78431374, a: 0.5019608}
    m_ColorMultiplier: 1
    m_FadeDuration: 0.1
  m_SpriteState:
    m_HighlightedSprite: {fileID: 0}
    m_PressedSprite: {fileID: 0}
    m_SelectedSprite: {fileID: 0}
    m_DisabledSprite: {fileID: 0}
  m_AnimationTriggers:
    m_NormalTrigger: Normal
    m_HighlightedTrigger: Highlighted
    m_PressedTrigger: Pressed
    m_SelectedTrigger: Selected
    m_DisabledTrigger: Disabled
  m_Interactable: 1
  m_TargetGraphic: {fileID: @@IMG@@}
  m_OnClick:
    m_PersistentCalls:
      m_Calls:@@CALLS@@
'@

function New-Button([string]$name, $fatherRt, [double]$x, [double]$y, [double]$w, [double]$h,
                    [string]$label, [double]$lsize, [string]$bg,
                    [string]$bindType, [string]$method, [int]$arg = -1) {
    $ui = New-UI $name $fatherRt $x $y $w $h
    $img = Add-Image $ui $bg 1
    $btn = Next-Id
    Add-Comp $ui $btn

    $callToken = ''
    if ($bindType -ne '' -and $script:compByType.ContainsKey($bindType)) {
        $idx = $script:bindings.Count
        [void]$script:bindings.Add(@{ target = $script:compByType[$bindType]; type = $bindType; method = $method; arg = $arg })
        $callToken = "`n@@T$idx@@"
    }
    Add-Raw ($T_BTN.Replace('@@BTN@@', "$btn").Replace('@@GO@@', "$($ui.go)").Replace('@@IMG@@', "$img").Replace('@@CALLS@@', $callToken))

    $labelUi = New-UI 'Label' $ui.rt 0 0 $w $h
    $null = Add-Text $labelUi $label $lsize '1, 1, 1, 1' 4
    return $ui
}

function Add-Script([hashtable]$ui, [string]$guid, [string]$extra = '') {
    $sid = Next-Id
    Add-Comp $ui $sid
    $raw = @"
--- !u!114 &$sid
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: $($ui.go)}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: $guid, type: 3}
  m_Name: 
  m_EditorClassIdentifier: $extra
"@
    Add-Raw $raw
    return $sid
}

$T_TMPT = @'
--- !u!114 &@@TX@@
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: @@GO@@}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: f4688fdb7df04437aeb418b961361dc5, type: 3}
  m_Name: 
  m_EditorClassIdentifier: Unity.TextMeshPro::TMPro.TextMeshProUGUI
  m_Material: {fileID: 0}
  m_Color: @@COLOR@@
  m_RaycastTarget: 0
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_text: @@TEXT@@
  m_isRightToLeft: 0
  m_fontAsset: {fileID: 11400000, guid: 8f586378b4e144a9851e7b34d9b748ee, type: 2}
  m_sharedMaterial: {fileID: 2180264, guid: 8f586378b4e144a9851e7b34d9b748ee, type: 2}
  m_fontSharedMaterials: []
  m_fontMaterial: {fileID: 0}
  m_fontMaterials: []
  m_fontColor32:
    serializedVersion: 2
    rgba: 4294967295
  m_fontColor: @@COLOR@@
  m_enableVertexGradient: 0
  m_colorMode: 3
  m_fontColorGradient:
    topLeft: {r: 1, g: 1, b: 1, a: 1}
    topRight: {r: 1, g: 1, b: 1, a: 1}
    bottomLeft: {r: 1, g: 1, b: 1, a: 1}
    bottomRight: {r: 1, g: 1, b: 1, a: 1}
  m_fontColorGradientPreset: {fileID: 0}
  m_spriteAsset: {fileID: 0}
  m_tintAllSprites: 0
  m_StyleSheet: {fileID: 0}
  m_TextStyleHashCode: 0
  m_overrideHtmlColors: 0
  m_faceColor:
    serializedVersion: 2
    rgba: 4294967295
  m_fontSize: @@SIZE@@
  m_fontSizeBase: @@SIZE@@
  m_fontWeight: 400
  m_enableAutoSizing: 0
  m_fontSizeMin: 18
  m_fontSizeMax: 72
  m_fontStyle: 0
  m_HorizontalAlignment: @@H@@
  m_VerticalAlignment: @@V@@
  m_textAlignment: 65535
  m_characterSpacing: 0
  m_characterHorizontalScale: 1
  m_wordSpacing: 0
  m_lineSpacing: 0
  m_lineSpacingMax: 0
  m_paragraphSpacing: 0
  m_charWidthMaxAdj: 0
  m_TextWrappingMode: @@WRAP@@
  m_wordWrappingRatios: 0.4
  m_overflowMode: 0
  m_linkedTextComponent: {fileID: 0}
  parentLinkedComponent: {fileID: 0}
  m_enableKerning: 0
  m_ActiveFontFeatures: 6e72656b
  m_enableExtraPadding: 0
  checkPaddingRequired: 0
  m_isRichText: 1
  m_EmojiFallbackSupport: 1
  m_parseCtrlCharacters: 1
  m_isOrthographic: 1
  m_isCullingEnabled: 0
  m_horizontalMapping: 0
  m_verticalMapping: 0
  m_uvLineOffset: 0
  m_geometrySortingOrder: 0
  m_IsTextObjectScaleStatic: 0
  m_VertexBufferAutoSizeReduction: 0
  m_useMaxVisibleDescender: 1
  m_pageToDisplay: 1
  m_margin: {x: 0, y: 0, z: 0, w: 0}
  m_isUsingLegacyAnimationComponent: 0
  m_isVolumetricText: 0
  m_hasFontAssetChanged: 0
  m_baseMaterial: {fileID: 0}
  m_maskOffset: {x: 0, y: 0, z: 0, w: 0}
'@

function Add-TmpText([hashtable]$ui, [string]$text, [double]$size, [string]$color, [int]$h = 1, [int]$v = 256, [int]$wrap = 0) {
    $pw = $script:rtData[$ui.rt].w
    $ph = $script:rtData[$ui.rt].h
    $go = Next-Id
    $rt = Next-Id
    $script:goData[$go] = @{ name = 'TmpText'; active = $true; comps = New-Object System.Collections.ArrayList }
    $script:goData[$go].comps.Add($rt) | Out-Null
    $script:rtData[$rt] = @{ go = $go; name = 'TmpText'; x = 0; y = 0; w = $pw; h = $ph; father = $ui.rt; children = New-Object System.Collections.ArrayList }
    $script:rtData[$ui.rt].children.Add($rt) | Out-Null
    $cr = Next-Id
    $script:goData[$go].comps.Add($cr) | Out-Null
    Add-Raw ($T_CR.Replace('@@CR@@', "$cr").Replace('@@GO@@', "$go"))
    $tx = Next-Id
    $script:goData[$go].comps.Add($tx) | Out-Null
    Add-Raw ($T_TMPT.Replace('@@TX@@', "$tx").Replace('@@GO@@', "$go").Replace('@@TEXT@@', $text).Replace('@@SIZE@@', "$size").Replace('@@COLOR@@', (Format-Color $color)).Replace('@@H@@', "$h").Replace('@@V@@', "$v").Replace('@@WRAP@@', "$wrap"))
    return $tx
}

$T_TMPIF = @'
--- !u!114 &@@IF@@
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: @@GO@@}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 2da0c512f12947e489f739169773d7ca, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  m_Navigation:
    m_Mode: 3
    m_WrapAround: 0
    m_SelectOnUp: {fileID: 0}
    m_SelectOnDown: {fileID: 0}
    m_SelectOnLeft: {fileID: 0}
    m_SelectOnRight: {fileID: 0}
  m_Transition: 1
  m_Colors:
    m_NormalColor: {r: 1, g: 1, b: 1, a: 1}
    m_HighlightedColor: {r: 0.9607843, g: 0.9607843, b: 0.9607843, a: 1}
    m_PressedColor: {r: 0.78431374, g: 0.78431374, b: 0.78431374, a: 1}
    m_SelectedColor: {r: 0.9607843, g: 0.9607843, b: 0.9607843, a: 1}
    m_DisabledColor: {r: 0.78431374, g: 0.78431374, b: 0.78431374, a: 0.5019608}
    m_ColorMultiplier: 1
    m_FadeDuration: 0.1
  m_SpriteState:
    m_HighlightedSprite: {fileID: 0}
    m_PressedSprite: {fileID: 0}
    m_SelectedSprite: {fileID: 0}
    m_DisabledSprite: {fileID: 0}
  m_AnimationTriggers:
    m_NormalTrigger: Normal
    m_HighlightedTrigger: Highlighted
    m_PressedTrigger: Pressed
    m_SelectedTrigger: Selected
    m_DisabledTrigger: Disabled
  m_Interactable: 1
  m_TargetGraphic: {fileID: @@IMG@@}
  m_TextViewport: {fileID: @@AREA@@}
  m_TextComponent: {fileID: @@TEXT@@}
  m_Placeholder: {fileID: @@PH@@}
  m_VerticalScrollbar: {fileID: 0}
  m_VerticalScrollbarEventHandler: {fileID: 0}
  m_LayoutGroup: {fileID: 0}
  m_ScrollSensitivity: 1
  m_ContentType: 0
  m_InputType: 0
  m_AsteriskChar: 42
  m_KeyboardType: 0
  m_LineType: 0
  m_HideMobileInput: 0
  m_HideSoftKeyboard: 0
  m_CharacterValidation: 0
  m_RegexValue: 
  m_GlobalPointSize: 26
  m_CharacterLimit: 200
  m_OnEndEdit:
    m_PersistentCalls:
      m_Calls: []
  m_OnSubmit:
    m_PersistentCalls:
      m_Calls: []
  m_OnSelect:
    m_PersistentCalls:
      m_Calls: []
  m_OnDeselect:
    m_PersistentCalls:
      m_Calls: []
  m_OnTextSelection:
    m_PersistentCalls:
      m_Calls: []
  m_OnEndTextSelection:
    m_PersistentCalls:
      m_Calls: []
  m_OnValueChanged:
    m_PersistentCalls:
      m_Calls: []
  m_OnTouchScreenKeyboardStatusChanged:
    m_PersistentCalls:
      m_Calls: []
  m_CaretColor: {r: 0.9, g: 0.9, b: 0.95, a: 1}
  m_CustomCaretColor: 1
  m_SelectionColor: {r: 0.35, g: 0.55, b: 1, a: 0.55}
  m_Text: 
  m_CaretBlinkRate: 0.85
  m_CaretWidth: 1
  m_ReadOnly: 0
  m_RichText: 1
  m_GlobalFontAsset: {fileID: 11400000, guid: 8f586378b4e144a9851e7b34d9b748ee, type: 2}
  m_OnFocusSelectAll: 1
  m_ResetOnDeActivation: 1
  m_KeepTextSelectionVisible: 0
  m_RestoreOriginalTextOnEscape: 1
  m_isRichTextEditingAllowed: 0
  m_LineLimit: 0
  isAlert: 0
  m_InputValidator: {fileID: 0}
  m_ShouldActivateOnSelect: 1
'@

$T_MASK = @'
--- !u!114 &@@MC@@
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: @@GO@@}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 3312d7739989d2b4e91e6319e9a96d76, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  m_Padding: {x: 0, y: 0, z: 0, w: 0}
  m_Softness: {x: 0, y: 0}
'@

$T_SCROLL = @'
--- !u!114 &@@SC@@
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: @@GO@@}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 1aa08ab6e0800fa44ae55d278d1423e3, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  m_Horizontal: 0
  m_Vertical: 1
  m_MovementType: 1
  m_Elasticity: 0.1
  m_Inertia: 1
  m_DecelerationRate: 0.135
  m_ScrollSensitivity: 30
  m_Viewport: {fileID: @@VIEW@@}
  m_Content: {fileID: @@CONTENT@@}
  m_HorizontalScrollbar: {fileID: 0}
  m_VerticalScrollbar: {fileID: 0}
  m_HorizontalScrollbarVisibility: 0
  m_VerticalScrollbarVisibility: 2
  m_HorizontalScrollbarSpacing: 0
  m_VerticalScrollbarSpacing: -3
  m_OnValueChanged:
    m_PersistentCalls:
      m_Calls: []
'@

$T_VLAYOUT = @'
--- !u!114 &@@LG@@
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: @@GO@@}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 59f8146938fff824cb5fd77236b75775, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  m_Padding:
    m_Left: 12
    m_Right: 12
    m_Top: 12
    m_Bottom: 12
  m_ChildAlignment: 0
  m_Spacing: 8
  m_ChildForceExpandWidth: 1
  m_ChildForceExpandHeight: 0
  m_ChildControlWidth: 1
  m_ChildControlHeight: 0
  m_ChildScaleWidth: 0
  m_ChildScaleHeight: 0
  m_ReverseArrangement: 0
'@

$T_FITTER = @'
--- !u!114 &@@CF@@
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: @@GO@@}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 3245ec927659c4140ac4f8d17403cc18, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  m_HorizontalFit: 0
  m_VerticalFit: 1
'@

function Add-TmpInput([string]$name, $fatherRt, [double]$x, [double]$y, [double]$w, [double]$h) {
    $ui = New-UI $name $fatherRt $x $y $w $h
    $img = Add-Image $ui '0.16, 0.19, 0.26, 1' 1

    $area = New-UI 'TextArea' $ui.rt 10 10 ($w - 20) ($h - 20)
    $mc = Next-Id
    Add-Comp $area $mc
    Add-Raw ($T_MASK.Replace('@@MC@@', "$mc").Replace('@@GO@@', "$($area.go)"))

    $textUi = @{ rt = $area.rt; w = ($w - 20); h = ($h - 20) }
    $textComp = Add-TmpText $textUi '' 26 '0.95, 0.96, 0.98, 1' 1 256
    $phUi = @{ rt = $area.rt; w = ($w - 20); h = ($h - 20) }
    $phComp = Add-TmpText $phUi '输入聊天内容…' 24 '0.55, 0.58, 0.64, 1' 1 256

    $ifc = Next-Id
    Add-Comp $ui $ifc
    Add-Raw ($T_TMPIF.Replace('@@IF@@', "$ifc").Replace('@@GO@@', "$($ui.go)").Replace('@@IMG@@', "$img").Replace('@@AREA@@', "$($area.rt)").Replace('@@TEXT@@', "$textComp").Replace('@@PH@@', "$phComp"))
    return @{ ui = $ui; inputComp = $ifc }
}

function New-Call([hashtable]$bind) {
    if ($bind.arg -lt 0) { $mode = '1'; $intLine = '          m_IntArgument: 0' }
    else { $mode = '3'; $intLine = "          m_IntArgument: $($bind.arg)" }
    $body = @"
      - m_Target: {fileID: $($bind.target)}
        m_TargetAssemblyTypeName: $($bind.type)
        m_MethodName: $($bind.method)
        m_Mode: $mode
        m_Arguments:
          m_ObjectArgument: {fileID: 0}
          m_ObjectArgumentAssemblyTypeName: UnityEngine.Object, UnityEngine
          $intLine
          m_FloatArgument: 0
          m_StringArgument: 
          m_BoolArgument: 0
        m_CallState: 2
"@
    return $body
}

function Resolve-Bindings() {
    $out = New-Object System.Collections.ArrayList
    for ($i = 0; $i -lt $script:blocks.Count; $i++) {
        $raw = $script:blocks[$i]
        for ($b = 0; $b -lt $script:bindings.Count; $b++) {
            $token = "@@T$b@@"
            if ($raw.Contains($token)) {
                $raw = $raw.Replace($token, (New-Call $script:bindings[$b]))
            }
        }
        [void]$out.Add($raw)
    }
    $script:blocks = $out
}

function Emit() {
    Resolve-Bindings
    $out = New-Object System.Collections.ArrayList
    foreach ($raw in $script:blocks) { [void]$out.Add($raw) }
    foreach ($goId in $script:goData.Keys) {
        $g = $script:goData[$goId]
        $c = ''
        foreach ($cid in $g.comps) { $c += "`n  - component: {fileID: $cid}" }
        $act = $(if ($g.active) { '1' } else { '0' })
        [void]$out.Add($T_GO.Replace('@@GO@@', "$goId").Replace('@@NAME@@', $g.name).Replace('@@COMPS@@', $c).Replace('@@ACTIVE@@', $act))
    }
    foreach ($rtId in $script:rtData.Keys) {
        $r = $script:rtData[$rtId]
        $c = ''
        foreach ($cid in $r.children) { $c += "`n  - {fileID: $cid}" }
        [void]$out.Add($T_RT.Replace('@@RT@@', "$rtId").Replace('@@GO@@', "$($r.go)").Replace('@@CHILDREN@@', $c).Replace('@@FATHER@@', "$($r.father)").Replace('@@X@@', "$($r.x)").Replace('@@Y@@', "-$($r.y)").Replace('@@W@@', "$($r.w)").Replace('@@H@@', "$($r.h)"))
    }
    return ($out -join "`n")
}

# ============ 颜色 ============
$C_DEEP   = '0.035, 0.05, 0.09, 1'
$C_TOPBAR = '0.025, 0.035, 0.06, 0.95'
$C_PANEL  = '0.055, 0.065, 0.10, 0.99'
$C_DIM    = '0, 0, 0, 0.65'
$C_ACCENT = '0.92, 0.42, 0.20, 1'
$C_BLUE   = '0.16, 0.32, 0.55, 1'
$C_GOLD   = '0.62, 0.46, 0.16, 1'
$C_RED    = '0.52, 0.20, 0.16, 1'
$C_WHITE  = '1, 1, 1, 1'
$C_SUB    = '0.66, 0.70, 0.76, 1'

# ============ LobbyCanvas ============
$canvas = New-UI 'LobbyCanvas' $null 0 0 1920 1080
$canvasGo = $canvas.go
$canvasRt = $canvas.rt

$canvasComp = Next-Id
Add-Comp $canvas $canvasComp
Add-Raw (@"
--- !u!223 &$canvasComp
Canvas:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: $canvasGo}
  m_Enabled: 1
  serializedVersion: 3
  m_RenderMode: 0
  m_Camera: {fileID: 0}
  m_PlaneDistance: 100
  m_PixelPerfect: 0
  m_ReceivesEvents: 1
  m_OverrideSorting: 0
  m_OverridePixelPerfect: 0
  m_SortingBucketNormalizedSize: 0
  m_AdditionalShaderChannelsFlag: 25
  m_UpdateRectTransformForStandalone: 0
  m_SortingLayerID: 0
  m_SortingOrder: 10
  m_TargetDisplay: 0
"@)

$scalerComp = Next-Id
Add-Comp $canvas $scalerComp
Add-Raw (@"
--- !u!114 &$scalerComp
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: $canvasGo}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 0cd44c1031e13a943bb63640046fad76, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  m_UiScaleMode: 1
  m_ReferencePixelsPerUnit: 100
  m_ScaleFactor: 1
  m_ReferenceResolution: {x: 1920, y: 1080}
  m_ScreenMatchMode: 0
  m_MatchWidthOrHeight: 0.5
  m_PhysicalUnit: 3
  m_FallbackScreenDPI: 96
  m_DefaultSpriteDPI: 96
  m_DynamicPixelsPerUnit: 1
  m_PresetInfoIsWorld: 0
"@)

$rayComp = Next-Id
Add-Comp $canvas $rayComp
Add-Raw (@"
--- !u!114 &$rayComp
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: $canvasGo}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: dc42784cf147c0c48a680349fa168899, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  m_IgnoreReversedGraphics: 1
  m_BlockingObjects: 0
  m_BlockingMask:
    serializedVersion: 2
    m_Bits: 4294967295
"@)

$lobbyUiComp = Add-Script $canvas 'af72b17e43a24d8faaaa000000000004' "`n  sourceFont: {fileID: 12800000, guid: 6ef9c88c2621fd24f9d16ec5d846dee7, type: 3}"
$script:compByType['LobbyUI'] = $lobbyUiComp

# ============ 主界面（王者荣耀风格） ============
$main = New-UI 'MainScreen' $canvasRt 0 0 1920 1080
$bgRawUi = New-UI 'LobbyBackground' $main.rt 0 0 1920 1080
$null = Add-SpriteImage $bgRawUi 'dd28e897e81653d3aabbccddeeff0011'
$mainDim = New-UI 'MainDim' $main.rt 0 0 1920 1080
$null = Add-Image $mainDim '0, 0, 0, 0.58' 0

$topBar = New-UI 'TopBar' $main.rt 0 0 1920 112
$null = Add-Image $topBar $C_TOPBAR 0

$avatar = New-UI 'Avatar' $topBar.rt 56 26 60 60
$null = Add-Image $avatar '0.30, 0.60, 0.42, 1' 0

$playerNameUi = New-UI 'PlayerName' $topBar.rt 150 24 700 46
$null = Add-Text $playerNameUi '代理人：' 30 $C_WHITE 3

$chip = New-UI 'ProgressChip' $topBar.rt 1440 28 220 56
$null = Add-Image $chip '0.10, 0.14, 0.19, 1' 0
$chipTextUi = New-UI 'ProgressText' $chip.rt 0 0 220 56
$null = Add-Text $chipTextUi '图鉴 0/12' 22 '0.95, 0.78, 0.35, 1' 4

$null = New-Button 'BackButton' $topBar.rt 1700 28 180 56 '返回登录' 22 $C_RED 'LobbyUI' 'BackToLogin'

$null = New-Button 'BattleMainButton' $main.rt 140 430 420 220 '战斗出击' 52 $C_ACCENT 'LobbyUI' 'ShowBattle'
$battleTipUi = New-UI 'BattleTip' $main.rt 150 690 700 40
$null = Add-Text $battleTipUi '选择怪物，进入对应战斗关卡' 22 '0.88, 0.90, 0.94, 1' 0

$null = New-Button 'CodexNavButton' $main.rt 1640 240 240 104 '角色图鉴' 28 $C_BLUE 'LobbyUI' 'ShowCodex'
$null = New-Button 'GachaNavButton' $main.rt 1640 370 240 104 '角色抽卡' 28 '0.78, 0.34, 0.18, 1' 'LobbyUI' 'ShowGacha'
$null = New-Button 'ChatNavButton' $main.rt 1640 500 240 104 '频道聊天' 28 $C_GOLD 'LobbyUI' 'ShowChat'

$verUi = New-UI 'VersionText' $main.rt 40 1042 700 30
$null = Add-Text $verUi '绝区零 · 代理人总控室 v0.1' 18 '0.88, 0.90, 0.94, 1' 0

# ============ 图鉴窗口 ============
$codexWin = New-UI 'CodexWindow' $canvasRt 0 0 1920 1080 $false
$null = Add-Image $codexWin $C_DIM 1
$script:compByType['CodexPanel'] = Add-Script $codexWin 'a1b2c3d4e5f60718aaaa000000000009'

$codexBg = New-UI 'CodexBg' $codexWin.rt 300 90 1320 900
$null = Add-Image $codexBg $C_PANEL 1
$codexTitleUi = New-UI 'CodexTitle' $codexBg.rt 44 28 900 50
$null = Add-Text $codexTitleUi '已拥有角色图鉴 (0/12)' 34 $C_WHITE 0
$null = New-Button 'CodexClose' $codexBg.rt 1230 24 66 66 '×' 34 $C_RED 'CodexPanel' 'Close'

$emptyHint = New-UI 'EmptyHint' $codexBg.rt 460 390 400 120 $false
$null = Add-Text $emptyHint '图鉴空空如也，先去抽卡吧！' 26 $C_SUB 1

$slots = New-UI 'Slots' $codexBg.rt 44 110 1230 770
for ($i = 0; $i -lt 12; $i++) {
    $col = $i % 4
    $row = [math]::Floor($i / 4)
    $sx = $col * 302
    $sy = $row * 252
    $slot = New-UI "Slot_$i" $slots.rt $sx $sy 280 230
    $null = Add-Image $slot '0.08, 0.09, 0.13, 0.95' 0
    $rarUi = New-UI 'Rarity' $slot.rt 14 16 252 34
    $null = Add-Text $rarUi '' 24 $C_WHITE 0
    $nmUi = New-UI 'Name' $slot.rt 14 62 252 42
    $null = Add-Text $nmUi '' 30 $C_WHITE 0
}

# ============ 抽卡窗口 ============
$gachaWin = New-UI 'GachaWindow' $canvasRt 0 0 1920 1080 $false
$null = Add-Image $gachaWin $C_DIM 1
$script:compByType['GachaPanel'] = Add-Script $gachaWin 'a1b2c3d4e5f60719aaaa000000000010'

$gachaBg = New-UI 'GachaBg' $gachaWin.rt 370 130 1180 820
$null = Add-Image $gachaBg $C_PANEL 1
$gachaTitleUi = New-UI 'GachaTitle' $gachaBg.rt 44 30 800 50
$null = Add-Text $gachaTitleUi '角色抽卡 · 信号检索' 34 $C_WHITE 0
$gachaTipUi = New-UI 'GachaTip' $gachaBg.rt 44 104 1000 40
$null = Add-Text $gachaTipUi '每次抽取随机获得一位代理人 · R（蓝）/ SR（紫）/ SSR（金）' 22 $C_SUB 0
$null = New-Button 'GachaClose' $gachaBg.rt 1086 24 66 66 '×' 34 $C_RED 'GachaPanel' 'Close'

$null = New-Button 'PullButton' $gachaBg.rt 120 300 400 130 '信号抽取 ×1' 42 $C_ACCENT 'GachaPanel' 'Pull'
$rateUi = New-UI 'RateText' $gachaBg.rt 120 480 400 36
$null = Add-Text $rateUi 'SSR 5%  ·  SR 25%  ·  R 70%' 20 $C_SUB 1
$ownedUi = New-UI 'OwnedText' $gachaBg.rt 120 540 700 44
$null = Add-Text $ownedUi '图鉴收集：0 / 12' 26 $C_WHITE 0
$null = New-Button 'ResetButton' $gachaBg.rt 120 620 240 58 '重置本地存档' 22 $C_RED 'GachaPanel' 'ResetProgress'

# ============ 战斗选择窗口 ============
$battleWin = New-UI 'BattleWindow' $canvasRt 0 0 1920 1080 $false
$null = Add-Image $battleWin $C_DIM 1
$script:compByType['BattlePanel'] = Add-Script $battleWin 'a1b2c3d4e5f60720aaaa000000000011'

$battleBg = New-UI 'BattleBg' $battleWin.rt 310 100 1300 880
$null = Add-Image $battleBg $C_PANEL 1
$battleTitleUi = New-UI 'BattleTitle' $battleBg.rt 44 28 800 50
$null = Add-Text $battleTitleUi '战斗出击 · 选择目标' 34 $C_WHITE 0
$battleTip2 = New-UI 'BattleTip' $battleBg.rt 44 92 1000 36
$null = Add-Text $battleTip2 '不同怪物拥有不同的攻击节奏与索敌距离' 22 $C_SUB 0
$null = New-Button 'BattleClose' $battleBg.rt 1206 24 66 66 '×' 34 $C_RED 'BattlePanel' 'Close'

$cardDefs = @(
    @{ n = '普通哥布林'; d = '空洞底层的普通怪物，反应平平，适合热身。'; s = '难度 ★☆☆'; b = '0.20, 0.30, 0.24, 1'; f = '0.24, 0.44, 0.30, 1' },
    @{ n = '精英哥布林'; d = '被以太侵蚀得更深，攻击更快、索敌更远。'; s = '难度 ★★☆'; b = '0.36, 0.17, 0.13, 1'; f = '0.60, 0.26, 0.18, 1' },
    @{ n = '狂暴哥布林'; d = '完全狂暴化的危险个体，几乎不给喘息机会。'; s = '难度 ★★★'; b = '0.31, 0.18, 0.38, 1'; f = '0.52, 0.28, 0.62, 1' }
)
for ($i = 0; $i -lt 3; $i++) {
    $d = $cardDefs[$i]
    $cx = 55 + $i * 415
    $card = New-UI "Card_$i" $battleBg.rt $cx 160 370 610
    $null = Add-Image $card $d.b 1
    $cn = New-UI 'Name' $card.rt 24 24 322 48
    $null = Add-Text $cn $d.n 34 $C_WHITE 0
    $cs = New-UI 'Stars' $card.rt 24 88 322 34
    $null = Add-Text $cs $d.s 22 '1, 0.84, 0.32, 1' 0
    $cd = New-UI 'Desc' $card.rt 24 138 322 190
    $null = Add-Text $cd $d.d 22 $C_SUB 0
    $null = New-Button 'FightButton' $card.rt 55 480 260 84 '进入战斗' 26 $d.f 'BattlePanel' 'EnterBattle' $i
}

# ============ 聊天窗口 ============
$chatWin = New-UI 'ChatWindow' $canvasRt 0 0 1920 1080 $false
$null = Add-Image $chatWin $C_DIM 1
$script:compByType['ChatPanel'] = Add-Script $chatWin 'a1b2c3d4e5f60721aaaa000000000012'

$chatBg = New-UI 'ChatBg' $chatWin.rt 260 60 1400 960
$null = Add-Image $chatBg $C_PANEL 1
$chatTitleUi = New-UI 'ChatTitle' $chatBg.rt 40 24 600 50
$null = Add-Text $chatTitleUi '世界频道聊天' 34 $C_WHITE 0
$null = New-Button 'ChatClose' $chatBg.rt 1306 24 66 66 '×' 34 $C_RED 'ChatPanel' 'Close'

$statusUi = New-UI 'StatusText' $chatBg.rt 740 98 620 40
$null = Add-Text $statusUi '正在连接频道服务器…' 20 $C_SUB 5

$scrollRoot = New-UI 'MessageScroll' $chatBg.rt 40 168 1320 610
$null = Add-Image $scrollRoot '0.035, 0.045, 0.07, 1' 1
$viewport = New-UI 'Viewport' $scrollRoot.rt 0 0 1320 610
$null = Add-Image $viewport '1, 1, 1, 0.01' 0
$mc = Next-Id
Add-Comp $viewport $mc
Add-Raw ($T_MASK.Replace('@@MC@@', "$mc").Replace('@@GO@@', "$($viewport.go)"))

$content = New-UI 'Content' $viewport.rt 0 0 1320 0
$lg = Next-Id
Add-Comp $content $lg
Add-Raw ($T_VLAYOUT.Replace('@@LG@@', "$lg").Replace('@@GO@@', "$($content.go)"))
$cf = Next-Id
Add-Comp $content $cf
Add-Raw ($T_FITTER.Replace('@@CF@@', "$cf").Replace('@@GO@@', "$($content.go)"))

$sc = Next-Id
Add-Comp $scrollRoot $sc
Add-Raw ($T_SCROLL.Replace('@@SC@@', "$sc").Replace('@@GO@@', "$($scrollRoot.go)").Replace('@@VIEW@@', "$($viewport.rt)").Replace('@@CONTENT@@', "$($content.rt)"))

$inputBox = Add-TmpInput 'ChatInput' $chatBg.rt 40 838 900 66
$null = New-Button 'SendButton' $chatBg.rt 960 838 180 66 '发送' 26 $C_ACCENT 'ChatPanel' 'Send'
$null = New-Button 'ReconnectButton' $chatBg.rt 1160 838 200 66 '重新连接' 22 $C_BLUE 'ChatPanel' 'Reconnect'

# ============ 抽卡结果弹窗 ============
$resultWin = New-UI 'GachaResultRoot' $canvasRt 0 0 1920 1080 $false
$null = Add-Image $resultWin '0, 0, 0, 0.72' 1
$resultPanel = New-UI 'ResultPanel' $resultWin.rt 650 140 620 800
$null = Add-Image $resultPanel $C_PANEL 1

$strip = New-UI 'ResultStrip' $resultPanel.rt 0 0 620 16
$null = Add-Image $strip '1, 0.79, 0.30, 1' 0
$rarUi = New-UI 'RarityText' $resultPanel.rt 40 60 540 70
$null = Add-Text $rarUi '' 52 $C_WHITE 1
$nmUi = New-UI 'NameText' $resultPanel.rt 40 170 540 60
$null = Add-Text $nmUi '' 40 $C_WHITE 1
$tagUi = New-UI 'TagText' $resultPanel.rt 40 250 540 44
$null = Add-Text $tagUi '' 28 $C_WHITE 1
$subUi = New-UI 'SubText' $resultPanel.rt 40 320 540 40
$null = Add-Text $subUi '' 24 $C_SUB 1
$descUi = New-UI 'DescText' $resultPanel.rt 60 400 500 180
$null = Add-Text $descUi '' 20 $C_WHITE 0
$null = New-Button 'ResultClose' $resultPanel.rt 200 660 220 80 '收下' 28 $C_ACCENT 'GachaPanel' 'HideResult'

# ============ EventSystem ============
$esGo = Next-Id
$esRt = Next-Id
$esComp = Next-Id
$esModule = Next-Id
Add-Raw (@"
--- !u!1 &$esGo
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: $esRt}
  - component: {fileID: $esComp}
  - component: {fileID: $esModule}
  m_Layer: 0
  m_Name: EventSystem
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &$esRt
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: $esGo}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 0}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!114 &$esComp
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: $esGo}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 76c392e42b5098c458856cdf6ecaaaa1, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  m_FirstSelected: {fileID: 0}
  m_sendNavigationEvents: 1
  m_DragThreshold: 10
--- !u!114 &$esModule
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: $esGo}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 01614664b831546d2ae94a42149d80ac, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  m_SendPointerHoverToParent: 1
  m_MoveRepeatDelay: 0.5
  m_MoveRepeatRate: 0.1
  m_XRTrackingOrigin: {fileID: 0}
  m_ActionsAsset: {fileID: 0}
  m_PointAction: {fileID: 0}
  m_MoveAction: {fileID: 0}
  m_SubmitAction: {fileID: 0}
  m_CancelAction: {fileID: 0}
  m_LeftClickAction: {fileID: 0}
  m_MiddleClickAction: {fileID: 0}
  m_RightClickAction: {fileID: 0}
  m_ScrollWheelAction: {fileID: 0}
  m_TrackedDevicePositionAction: {fileID: 0}
  m_TrackedDeviceOrientationAction: {fileID: 0}
  m_DeselectOnBackgroundClick: 1
  m_PointerBehavior: 0
  m_CursorLockBehavior: 0
  m_ScrollDeltaPerTick: 6
"@)

# ============ LobbySystem（聊天服务器 + 客户端） ============
Add-Raw (@"
--- !u!1 &910000001
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 910000002}
  - component: {fileID: 910000004}
  - component: {fileID: 910000005}
  m_Layer: 0
  m_Name: LobbySystem
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &910000002
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 910000001}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 0}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!114 &910000004
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 910000001}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: e7dc979a01db42c2aaaa000000000007, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  ipAddress: 127.0.0.1
  port: 54011
--- !u!114 &910000005
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 910000001}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 893c2fbb306a4c7caaaa000000000008, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  ipAddress: 127.0.0.1
  port: 54011
  connectTimeout: 5
  autoConnectOnStart: 1
"@)

# ============ 保留旧场景对象（相机/灯光）并输出 ============
$oldSceneText = Get-Content $lobbyScene -Raw
$oldLines = $oldSceneText -split "`r?`n"
$oldBlocks = New-Object System.Collections.ArrayList
$cur = New-Object System.Collections.ArrayList
foreach ($ln in $oldLines) {
    if ($ln -match '^--- !u!') {
        if ($cur.Count -gt 0) { [void]$oldBlocks.Add(($cur -join "`n")); $cur = New-Object System.Collections.ArrayList }
    }
    [void]$cur.Add($ln)
}
if ($cur.Count -gt 0) { [void]$oldBlocks.Add(($cur -join "`n")) }

$keep = New-Object System.Collections.ArrayList
foreach ($b in $oldBlocks) {
    $hdr = [regex]::Match($b, '^--- !u!\d+ &(\d+)')
    $oldId = 0
    if ($hdr.Success) { $oldId = [long]$hdr.Groups[1].Value }
    # 只保留 %YAML 头部与场景设置块（1~4），其余全部重新生成
    $skip = -not ($oldId -eq 0 -or ($oldId -ge 1 -and $oldId -le 4))
    if (-not $skip) { [void]$keep.Add($b) }
}

$newContent = Emit
$cameraLightBase = @"
--- !u!1 &19542680
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 19542682}
  - component: {fileID: 19542681}
  m_Layer: 0
  m_Name: Directional Light
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!108 &19542681
Light:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 19542680}
  m_Enabled: 1
  serializedVersion: 12
  m_Type: 1
  m_Color: {r: 1, g: 0.95686275, b: 0.8392157, a: 1}
  m_Intensity: 1
  m_Range: 10
  m_SpotAngle: 30
  m_InnerSpotAngle: 21.80208
  m_CookieSize2D: {x: 0.5, y: 0.5}
  m_Shadows:
    m_Type: 2
    m_Resolution: -1
    m_CustomResolution: -1
    m_Strength: 1
    m_Bias: 0.05
    m_NormalBias: 0.4
    m_NearPlane: 0.2
  m_CullingMask:
    serializedVersion: 2
    m_Bits: 4294967295
  m_RenderingLayerMask: 1
  m_Lightmapping: 4
  m_AreaSize: {x: 1, y: 1}
  m_BounceIntensity: 1
  m_ColorTemperature: 6570
  m_UseColorTemperature: 0
  m_ShadowRadius: 0
  m_ShadowAngle: 0
--- !u!4 &19542682
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 19542680}
  serializedVersion: 2
  m_LocalRotation: {x: 0.40821788, y: -0.23456968, z: 0.10938163, w: 0.8754261}
  m_LocalPosition: {x: 0, y: 3, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 0}
  m_LocalEulerAnglesHint: {x: 50, y: -30, z: 0}
--- !u!1 &593741535
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 593741538}
  - component: {fileID: 593741537}
  - component: {fileID: 593741536}
  m_Layer: 0
  m_Name: Main Camera
  m_TagString: MainCamera
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!81 &593741536
AudioListener:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 593741535}
  m_Enabled: 1
--- !u!20 &593741537
Camera:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 593741535}
  m_Enabled: 1
  serializedVersion: 2
  m_ClearFlags: 2
  m_BackGroundColor: {r: 0.1, g: 0.12, b: 0.18, a: 1}
  m_projectionMatrixMode: 1
  m_GateFitMode: 2
  m_FOVAxisMode: 0
  m_SensorSize: {x: 36, y: 24}
  m_LensShift: {x: 0, y: 0}
  m_NormalizedViewPortRect:
    serializedVersion: 2
    x: 0
    y: 0
    width: 1
    height: 1
  near clip plane: 0.3
  far clip plane: 1000
  field of view: 60
  orthographic: 0
  orthographic size: 5
  m_Depth: -1
  m_CullingMask:
    serializedVersion: 2
    m_Bits: 4294967295
  m_RenderingPath: -1
  m_TargetTexture: {fileID: 0}
  m_TargetDisplay: 0
  m_TargetEye: 3
  m_HDR: 1
  m_AllowMSAA: 1
  m_AllowDynamicResolution: 0
  m_ForceIntoRT: 0
  m_OcclusionCulling: 1
  m_StereoConvergence: 10
  m_StereoSeparation: 0.022
--- !u!4 &593741538
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 593741535}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 1, z: -10}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 0}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
"@

$sceneRootsText = @"
--- !u!1660057539 &9223372036854775807
SceneRoots:
  m_ObjectHideFlags: 0
  m_Roots:
  - {fileID: 593741538}
  - {fileID: 19542682}
  - {fileID: $canvasRt}
  - {fileID: $esRt}
  - {fileID: 910000002}
"@
$final = (($keep -join "`n") + "`n" + $cameraLightBase.TrimEnd("`n", "`r") + "`n" + $newContent.TrimEnd("`n", "`r") + "`n" + $sceneRootsText.TrimStart("`n", "`r") + "`n")
[System.IO.File]::WriteAllText($lobbyScene, $final, (New-Object System.Text.UTF8Encoding($false)))
