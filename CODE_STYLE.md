# Code style for the plugin repos

These rules apply to this repo and to every repo that imports `build/KCES2.Fusebox.props`: C# files, `.csproj`, `.projitems`, `.props`, `.targets` and `.slnx` files, in `src/`, `tests/`, `build/` and `analyzers/`.

The build enforces every rule that a tool can check: warnings are errors. The rules marked "convention" have no tool check. Authors apply them, and reviewers check them.

## Comments

The comments are in English only. If a Japanese text inside a comment is data, it stays. An example is a quoted game string (`"ああ。" = "Yeah."`).

1. Articles. Drop "the", "a" and "an" where able in comments, unless the sentence becomes ambiguous.
2. End punctuation. Every summary, remark sentence and `//` comment ends with a period.
3. Content to delete:
   - history and decisions: dates, "user decision", "first version did X", test results
   - process: "stays 0.1.0 during development", "raised on release"
   - text that repeats the member name or kind: "The list of ...", "The type of the setting's value"
   - text that repeats the code below it
   - internal labels that the code already shows: feature names, config keys, log strings
4. Content to keep, short, in `<remarks>`: the reason for the code, KCES2 behavior that the code depends on, non-obvious constraints (frame order, threads, Unity lifecycle), units and ranges, failure behavior.
5. Abbreviations and symbols: a closed list.
   - Words: `e.g.`, `i.e.`, `vs.`, `etc.`, `max`, `min`.
   - Units: ms, s, px, m, %.
   - ASCII symbols: `->` for "then" or "becomes", `+` between two calls or values, `=` for "equals", `x10` for "ten times".
   - No other abbreviation (no "w/", "b/c", "cfg", "impl"), except an identifier.
6. Summary shape:
   - Types, fields and properties: a noun phrase ("Plugin GUID.").
   - Methods: a verb phrase in the third person ("Finds KCES2 drag handlers.").
   - Bool properties and fields: "Indicates whether ...".
   - Predicate methods (the bool answer is the purpose, e.g. `IsOpen`, `HasJapanese`): "Determines whether ...".
   - Try methods with an `out` parameter: "Tries to ...". The `out` value goes into `<param>`.
   - Action methods that also return bool (a success flag, or a Harmony prefix that returns "run original"): a verb phrase for the action, and always a `<returns>`.
   - These verbs come from the .NET API docs ("Determines whether the specified file exists."). The .NET docs repeat the condition in `<returns>` or `<value>`. We do not: a bool `<returns>` or `<value>` exists only for an action method or for a fact that the summary does not give.
   - One line, about 12 words at most. More text goes into `<remarks>`.
   - Public members keep a summary (CS1591). If the summary of a private or internal member only repeats the name, the doc comment goes.
7. `<param>` and `<returns>`: if they give a fact that the name and type do not give, they stay (units, range, meaning of null or 0, side effects). Else they go. If one parameter of a method has a tag, all its parameters get one (CS1573), with the shortest text for the others.
8. No section dividers (`// ---- Name.`) between members. The member order (SA1201-SA1204) groups by kind and access, so a divider no longer matches the members after it. Files and folders group by feature.
9. Grammar. Docs, READMEs and commit messages follow Simplified Technical English (ASD-STE100). Code comments follow the rules above instead, and keep these STE rules: active voice where there is a verb, simple tenses, no filler words (for example crucial, robust, seamless, leverage), ASCII characters only (no em-dash, arrow, smart quote, ellipsis character).

Example:

```csharp
/// <summary>Mouse wheel on value row handle changes value.</summary>
/// <remarks>
/// One notch calls OnHandleDragStart + OnHandleDragEnd(Step, x10 with Shift): KCES2 value events and Undo work as for drag.
/// Receiver on handle object stops panel ScrollRect from scrolling too. Four-arrow (X+Y) control not handled.
/// </remarks>
```

## XML doc tags

The tags are Microsoft's recommended tags (https://learn.microsoft.com/dotnet/csharp/language-reference/xmldoc/recommended-tags).

1. Bool text in `<returns>` and `<value>` uses the .NET form: `<see langword="true"/> if ...; otherwise, <see langword="false"/>.` A text that gives only one extra fact can be short: `<see langword="false"/> also for <see langword="null"/>.`
2. Code names in doc text:
   - `<see cref="..."/>` for every type or member that the compiler can resolve: our code and the public API of KCES2, Unity, BepInEx and HarmonyX. The build checks each cref.
   - `<paramref name="..."/>` and `<typeparamref name="..."/>` for parameters.
   - `<see langword="..."/>` for `true`, `false`, `null` and other C# keywords.
   - `<c>` for the rest: private KCES2 members (cref cannot reach them), GameObject paths, file names, expressions, literal values.
   - `//` comments stay plain text.
3. Lists and paragraphs:
   - A list needs 3 or more parallel items. Fewer items stay in a sentence.
   - `<list type="number">` for ordered steps or ranked lookups. `<list type="bullet">` for unordered items.
   - An item with a name and an explanation uses `<term>` + `<description>`. Else `<description>` only.
   - `<list type="table">` with `<listheader>` only for real 2-column tables (e.g. key -> meaning).
   - `<para>` only if `<remarks>` has 2 or more paragraphs. A single paragraph has no `<para>`.
   - `<summary>` never has `<para>` or `<list>`.
4. Other tags:
   - `<exception cref="...">` for each exception that a method throws on purpose and that its doc mentions. The text moves out of `<remarks>`.
   - `<inheritdoc/>` on overrides and interface implementations, in place of a copied text.
   - `<example>` + `<code>` for usage samples.
   - `<value>` only per rule 6 of "Comments".
   - `<seealso cref="..."/>`: only on types, only as a backward link. Type A gets it when type B depends on a contract of A (call order, shared state, a return value that B acts on), because the code of A does not name B. Max 3 per type. Not for helpers that many types use (`LabelStyle`, `CommonLog`, `LogPaths`), not from tests to the tested type. Each link must be visible in the code of B.
   - Not used: `<include>`, `<permission>`.
5. Order: `<summary>`, `<typeparam>`, `<param>`, `<returns>`, `<value>`, `<exception>`, `<remarks>`, `<example>`, `<seealso>`.
6. Layout:
   - A tag whose text fits on one line (160 characters) stays on one line.
   - A longer `<param>`, `<returns>` or `<exception>` wraps. Continuation lines have no extra indent.
   - A `<remarks>` with one paragraph that fits on one line stays on one line. A `<remarks>` with 2 or more lines, `<para>` or `<list>` has its opening and closing tags on their own lines.
   - `<list>` and `<example>` always have their opening and closing tags on their own lines. Each `<item>` starts on its own line. `<para>` wraps its text.

Example:

```csharp
/// <summary>Reads TSV rows.</summary>
/// <param name="path">File path.</param>
/// <param name="headers">Accepted first lines; empty = header not checked.</param>
/// <returns>Rows, or <see langword="null"/> when file is unreadable.</returns>
/// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
/// <remarks>
/// <para>Rows with too few columns are skipped and counted.</para>
/// <para>Encoding: UTF-8; Shift-JIS bytes give warning.</para>
/// </remarks>
/// <seealso cref="TranslationReloader"/>
```

## Types and files

1. One top-level type per file: class, struct, interface, enum, record and delegate, also small ones (StyleCop SA1402 with all kinds).
2. File name = type name (SA1649). A generic type uses the StyleCop form `Foo{T}.cs`. No generic type exists today.
3. A nested type stays in its parent. If a nested type is longer than 150 lines, it goes into the partial file `<Parent>.<Nested>.cs`, and the parent becomes `partial` (convention).
4. A split type stays in the folder of its old file.
5. A folder with more than 12 C# files gets feature subfolders. The namespace follows the folder: project name plus the subfolder names (IDE0130, SDK default `RootNamespace`).
   Shared source (`.projitems`) uses the namespace of its shared project (`KCES2.Fusebox`), whatever project imports it. Rider checks it against the importing project, so each shared file has `// ReSharper disable once CheckNamespace` above its namespace.
6. `ConfigurationManagerAttributes` keeps its exact name, because ConfigurationManager finds the class by its name.
7. File-scoped namespaces (IDE0161). Implicit usings on (`ImplicitUsings` = `enable`). On net48 the SDK adds `System`, `System.Collections.Generic`, `System.IO`, `System.Linq`, `System.Threading` and `System.Threading.Tasks`.
8. Nullable reference types: off in plugins (`Nullable` = `disable`), because a destroyed Unity object is not C# null and annotations mislead there. Shared source (`src/KCES2.Fusebox/`) starts each file with `#nullable enable`, because other projects compile it, some with nullable on. The net48 reference assemblies have no null annotations (`string.IsNullOrEmpty` has no `[NotNullWhen(false)]`): after such a check, put `!` on the first use of the value.

## Member order

StyleCop order, enforced:

1. Kind (SA1201): fields, constructors, finalizer, delegates, events, enums, interfaces, properties, indexers, methods, nested structs, nested classes.
2. Access inside each kind (SA1202): public, internal, protected internal, protected, private protected, private.
3. Constants before fields (SA1203). Static before instance (SA1204).
4. No visible instance fields (CA1051): use properties. Constants and static readonly fields stay fields.
5. The empty constructor comes first (convention).
6. Classes declare their constructors explicitly (KFB0008). Primary constructors are allowed only on records and structs.

## Naming

Microsoft's "C# identifier naming rules and conventions", with one change: every private field uses `_camelCase`, also a static or static readonly field (no `s_`).

| Symbol                                                                | Style            | Example                 |
|-----------------------------------------------------------------------|------------------|-------------------------|
| Namespace, type, method, property, event, enum member, local function | PascalCase       | `ScrollValue`, `Attach` |
| Interface                                                             | `I` + PascalCase | `IEditSlot`             |
| Type parameter                                                        | `T` + PascalCase | `TValue`                |
| Constant (all access levels)                                          | PascalCase       | `MaxRows`               |
| Public, internal or protected field                                   | PascalCase       | `Guid`                  |
| Private field (instance, static, static readonly)                     | `_camelCase`     | `_wheelOn`, `_seen`     |
| Parameter, local, local constant                                      | camelCase        | `handle`, `isInt`       |

Exceptions:

- Harmony patch methods: their parameters keep the names that Harmony needs (`__instance`, `__result`, `___field`) and the names of the patched method's parameters. Our analyzer suppresses IDE1006 for every parameter of a patch method (KFBP0001). A patch method is a method with a Harmony attribute, a method named Prefix, Postfix, Transpiler, Finalizer, Prepare, Cleanup or TargetMethod(s) in a class with a Harmony attribute, a method with a parameter that starts with `__`, or a method named in a `nameof` in a file with `using HarmonyLib` (registration through `Harmony.Patch`).
- Test stubs of classes of the patched program keep the field names of the original class, because the tested code reads them by reflection. The stub class gets `[SuppressMessage("Style", "IDE1006", Justification = "KCES2 field names, read by reflection.")]`. If the original fields are not readonly, the stub also gets `[SuppressMessage("Style", "IDE0044", Justification = "KCES2 fields are not readonly; tests set them by reflection.")]`.
- Test methods use Microsoft's unit test pattern `Method_Scenario_Expected`, for example `Register_First_WinsAndLogs`. Roslyn's `pascal_case` check accepts words joined by "_", so no exception is necessary.

## Formatting

Base: Microsoft's template (`dotnet new editorconfig`, SDK 10).

1. Allman braces, 4 spaces in C#, 2 spaces in XML files, LF line ends, UTF-8 without BOM, final newline, no trailing whitespace.
2. Line length: 160 characters max, comments included (own analyzer). Characters are counted, not display columns. A string literal or URL that cannot break is exempt.
3. Braces: a one-line guard such as `if (input == null) return;` is allowed. If the statement is on its own line, braces are required (KFB0006). If the body spans several lines, braces are required (IDE0011, `when_multiline`). IDE0011 alone does not flag a one-line body on the next line.
4. `var` (IDE0007, IDE0008): if the type is apparent from the right side, use `var`. Built-in types and other types are explicit. If the type is on the left, use target-typed `new()` (IDE0090).
5. Blank lines:
   - One blank line separates the steps of a method body (convention). The guard clauses at the start of a method are one step. A guard on the result of the line before it stays in that step. A log line stays with the action that it reports.
   - No two blank lines in a row (IDE2000). No blank line between consecutive braces (IDE2002). No blank line directly after `{` or before `}` (KFB0002).
6. Other style rules: every "suggestion" or "warning" rule of the template is an error. IDE0046 (conditional expression for return) stays an error, but a method with a chain of guard returns, where the fix gives nested ternaries, keeps its `if` form with `[SuppressMessage("Style", "IDE0046", Justification = "Guard chain reads better as ifs.")]`. From the "silent" rules, these are errors:
   - no `this.`
   - `int`, not `Int32`
   - explicit access modifiers
   - parentheses for clarity
   - expression bodies only on a single line: methods, properties, accessors, local functions, never constructors
   - no unused usings (IDE0005)
7. Unity objects: the Unity analyzers (USP suppressors) stop `??`, `?.` and `is null` fixes on `UnityEngine.Object` values, because Unity overrides `==` for destroyed objects. `TrackedReference` values (for example `AnimationState`) have the same `==`, but no suppressor covers them: keep the explicit check with `[SuppressMessage("Style", "IDE0031", Justification = "...")]` on the member.

Example (a method body with steps, line length 160):

```csharp
private static void Attach(KCES2EditSliderHandle handle)
{
    if (!_seen.Add(handle.GetInstanceID())) return;

    Component input = handle.GetComponentInParent<KCES2IntSlideValueInput>(true);
    bool isInt = input != null;
    if (!isInt) input = handle.GetComponentInParent<KCES2FloatSlideValueInput>(true);
    if (input == null) return;

    var receiver = handle.gameObject.AddComponent<Receiver>();
    receiver.Init(handle, input, isInt ? _intStart : _floatStart, isInt ? _intEnd : _floatEnd);
    if (_attached++ == 0) Plugin.Log.LogInfo($"Wheel value change attached (first: {input.GetType().Name} {ParentName(input)})");
}
```

## Unity pitfalls

No tool finds these errors, unless the rule names one. Reviewers look for them in every plugin.

1. Destroyed objects. A destroyed Unity object is not C# `null`. Only Unity's `==` gives `true` for it.
   - `?.`, `??`, `??=` and `is null` skip Unity's `==` (UNT0007, UNT0008, UNT0023, UNT0029, and Formatting rule 7).
   - A Unity object in an `object`, interface or unconstrained generic variable also skips it. Cast the value to `UnityEngine.Object` before the comparison.
2. Cost of `==`. Each `==` on a Unity object calls native code. In a per-frame loop over many objects, keep the result or the instance ID. Rider marks this comparison in code that `Update` or a coroutine reaches.
3. Native memory. The garbage collector does not free a `Texture2D`, `RenderTexture`, `Mesh` or `Material` that plugin code makes. Call `Destroy` on it after its last use.
   - `Renderer.material` and `MeshFilter.mesh` make a copy at the first call. Each copy needs `Destroy` too.
   - `sharedMaterial` and `sharedMesh` change the asset for every object that uses it.
4. Main thread. Call the Unity API only from the main thread. Some KCES2 loaders call plugin code from other threads. Put that work in a queue that `Update` reads.
5. Coroutines. When its object is disabled or destroyed, a coroutine stops without a message.
   - An exception also stops it, and only the Unity log shows the exception. Catch exceptions inside the coroutine and give them to `FeatureGuard`.
   - C# does not allow `yield return` in a `try` that has a `catch`. Put the `try` around the code between two `yield` statements (`KCES2.SkipIntro`, `StartupSkip.WaitThenNext`).
6. Paused time. When `Time.timeScale` is 0, `Time.deltaTime` is 0 and `WaitForSeconds` never ends. Plugin timers and plugin UI use `Time.unscaledDeltaTime` and `WaitForSecondsRealtime`.
7. Scene loads. A scene load destroys every object, except root objects with `DontDestroyOnLoad`. Remove handlers from static events (for example `SceneManager.sceneLoaded`) in `OnDestroy`. Else the event calls into a destroyed object.
8. Allocations per frame. The Mono garbage collector in Unity is not generational, so small allocations in every frame cause frame-time spikes. Keep LINQ, string concatenation, closures, `GetComponents`, `FindObjectsOfType` and `GameObject.Find` out of `Update`.
9. Harmony and inlining. The Mono JIT can copy a small method (for example a getter) into its callers. A patch then has no effect at those calls, but the log still reports the patch. Patch the caller in that case.
   - Native methods (`InternalCall`) take no normal patch.
   - Do not patch methods of Unity structs: a patch on `ScriptableRenderContext.Cull` crashed CRC3D3.
10. Missing types. Mono compiles a whole method at its first call. A method can name a type or member that the running program does not have. Then the exception comes before the first line, outside every `try`. In a plugin for both programs, put code for one program in its own method with `[MethodImpl(MethodImplOptions.NoInlining)]`.

## Tooling

1. Roslyn analyzers: `EnforceCodeStyleInBuild` for the IDE rules, IDE1006 (naming) and IDE0055 (formatting). `EnableNETAnalyzers` for CA1051 (net48 has the .NET analyzers off by default). IDE0320 (static anonymous function) is a convention: the rule file sets it, but the command-line build does not report it (SDK 10.0.113). The IDE reports it.
2. StyleCop.Analyzers 1.2.0-beta.556, with only SA1201, SA1202, SA1203, SA1204, SA1402 and SA1649 on.
3. Own analyzer `KCES2.Fusebox.Style` in `analyzers/KCES2.Fusebox.Style/`:
   - line length 160 for C# (KFB0001)
   - no blank line directly after `{` or before `}` (KFB0002)
   - LF in C# files (KFB0005)
   - braces for a statement on its own line (KFB0006)
   - no primary constructor on a class (KFB0008). Roslyn has no rule for this: IDE0290 only suggests primary constructors, and the rule file turns it off.
   - `.csproj`, `.projitems`, `.props`, `.targets` and `.slnx`: well-formed XML (KFB0003), 2-space indent, LF, final newline, no trailing spaces (KFB0004)
   - no BOM in C# files and in the build files above (KFB0007). This check is not part of the analyzer: a task in `build/KCES2.Fusebox.props` reads the first bytes of each file before compiling. The IDE does not give analyzers the real file encoding.
   - Harmony parameter suppressor for IDE1006
4. One rule file `build/CodeStyle.globalconfig`, loaded by `build/KCES2.Fusebox.props` for every repo that imports it. Each repo keeps a short `.editorconfig` for the editor (charset, line ends, indent) and a `.gitattributes`.
5. Test projects compile linked plugin files (`<Compile Include=... Link=...>`). The plugins target net48, which has no `System.Index` or `System.Range`, so `build/Tests.props` turns IDE0056 and IDE0057 off for linked files only (generated `obj/.../LinkedFiles.globalconfig`). Test-only code keeps both rules.
6. Line-end rules apply to code and build files only. Data files keep their bytes: the KAG scripts (`.ks`) are CRLF with BOM, as in the original game files, and XUnity translation files can have a BOM.
7. Tests use AwesomeAssertions (`build/Tests.props`) for checks. Two xUnit calls stay: `Assert.NotNull` where the IDE needs null information after the check, and `Assert.Skip` to skip a test.
