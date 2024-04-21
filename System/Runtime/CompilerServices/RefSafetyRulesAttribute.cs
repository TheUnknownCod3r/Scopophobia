// Decompiled with JetBrains decompiler
// Type: System.Runtime.CompilerServices.RefSafetyRulesAttribute
// Assembly: Scopophobia, Version=1.1.1.0, Culture=neutral, PublicKeyToken=null
// MVID: 832B2E03-5C0C-4019-8402-65D9E2D1AD81
// Assembly location: G:\jaspercreations-Scopophobia-1.1.1\Scopophobia\Scopophobia.dll

using Microsoft.CodeAnalysis;
using System.Runtime.InteropServices;

namespace System.Runtime.CompilerServices
{
  [CompilerGenerated]
  [AttributeUsage(AttributeTargets.Module, AllowMultiple = false, Inherited = false)]
  internal sealed class RefSafetyRulesAttribute : Attribute
  {
    public readonly int Version;

    public RefSafetyRulesAttribute([In] int obj0) => this.Version = obj0;
  }
}
