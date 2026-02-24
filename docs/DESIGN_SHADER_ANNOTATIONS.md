# Shader Annotation System

## Problem

Shader interface attachment is currently split across three mechanisms:
1. Convention-based name matching in the asset pipeline (vertex/instance composable interfaces)
2. Manual partial class declarations in `ShaderExtensions/Shaders.cs` (shader-level interfaces like `IDaylightShader`)
3. The `// @instanced` annotation in GLSL (instance vs vertex attribute)

This proposal unifies (1) and (2) into a single `// @implements` GLSL annotation system, keeping everything derived from the shader source.

## Annotation Syntax

```glsl
// @implements(InterfaceName.PropertyName)
```

Placed on the line before a `uniform` or `layout(location=N) in` declaration.

### On vertex/instance attributes

```glsl
// @implements(IWithPosition3D.Position)
// @instanced
layout(location = 0) in vec3 aPosition;
```

The pipeline generates an explicit interface implementation on the `Vertex` or `Instance` struct:
- Getter: delegates to the record field
- Static `With` method: uses `with` expression

```csharp
Vector3D<float> IWithPosition3D<Vertex>.Position => aPosition;
static Vertex IWithPosition3D<Vertex>.WithPosition(Vertex self, Vector3D<float> position)
    => self with { aPosition = position };
```

### On uniforms

```glsl
// @implements(ICameraPositionShader.View)
uniform mat4 view;

// @implements(ICameraPositionShader.Projection)
uniform mat4 projection;
```

The generated shader class already has matching properties (`View`, `Projection`). The pipeline collects all `@implements` annotations, groups by interface name, and adds the interface to the shader class declaration when all annotated properties are present.

No explicit implementation needed — uniform properties satisfy the interface implicitly.

## Interface Conventions

Interfaces must follow these patterns for the pipeline to generate correct implementations:

**Vertex/instance interfaces** (immutable, on record structs):
```csharp
public interface IWithX<TSelf> where TSelf : IWithX<TSelf>
{
    SomeType X { get; }
    static abstract TSelf WithX(TSelf self, SomeType x);
}
```
- Property name matches the annotation's property name
- `With` method name is `With` + property name
- Parameter type is derived from the GLSL type

**Shader-level interfaces** (mutable, on shader classes):
```csharp
public interface ISomeShader
{
    SomeType? PropertyName { get; set; }
}
```
- Property names match the generated uniform properties (PascalCase of GLSL name)
- Nullable since shader uniforms are nullable

## Pipeline Behavior

1. Parse `// @implements(Interface.Property)` annotations (same line-before pattern as `@instanced`)
2. Group by interface name and target (attribute → struct, uniform → class)
3. For attributes: generate explicit interface implementations (getter + With)
4. For uniforms: add interface to the shader class declaration (implicit satisfaction)
5. Compile errors catch mismatches between annotation and actual interface definition

## Scope

This replaces:
- Convention-based semantic mapping in `ProcessShaders.cs` (`SemanticMap`, `NormalizeAttributeName`)
- Manual `ShaderExtensions/Shaders.cs` partial declarations
- Individual shader extension interfaces can stay as-is or be decomposed into per-uniform interfaces (project decision, not required)

The `// @instanced` annotation remains unchanged.

## Interface Location

Interfaces can live anywhere the game project can reference:
- **Engine** (`Olve.Engine3D.Rendering`): `IWithPosition3D`, `IWithNormal3D`, `IWithView`, etc.
- **Game** (`Olve.Trains`): `IDaylightShader`, `IWorldMousePositionShader`, etc.

The pipeline doesn't need to know where interfaces are defined — it just emits the name. Resolution happens at compile time.
