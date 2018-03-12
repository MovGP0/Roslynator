﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslynator.CSharp.Refactorings;

namespace Roslynator.CSharp.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SimpleAssignmentExpressionAnalyzer : BaseDiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(
                    DiagnosticDescriptors.UseCompoundAssignment,
                    DiagnosticDescriptors.UseCompoundAssignmentFadeOut);
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            base.Initialize(context);

            context.RegisterSyntaxNodeAction(AnalyzeSimpleAssignment, SyntaxKind.SimpleAssignmentExpression);
        }

        private static void AnalyzeSimpleAssignment(SyntaxNodeAnalysisContext context)
        {
            var assignment = (AssignmentExpressionSyntax)context.Node;

            if (UseCompoundAssignmentAnalysis.CanRefactor(assignment))
            {
                var binaryExpression = (BinaryExpressionSyntax)assignment.Right;

                context.ReportDiagnostic(DiagnosticDescriptors.UseCompoundAssignment, assignment, UseCompoundAssignmentAnalysis.GetCompoundOperatorText(binaryExpression));
                context.ReportNode(DiagnosticDescriptors.UseCompoundAssignmentFadeOut, binaryExpression.Left);
            }
        }
    }
}