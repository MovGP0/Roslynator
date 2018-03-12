﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslynator.CSharp.Refactorings
{
    internal static class MergeSwitchSectionsAnalysis
    {
        public static void AnalyzeSwitchStatement(SyntaxNodeAnalysisContext context)
        {
            var switchStatement = (SwitchStatementSyntax)context.Node;

            if (switchStatement.ContainsDiagnostics)
                return;

            SyntaxList<SwitchSectionSyntax> sections = switchStatement.Sections;

            if (sections.Count <= 1)
                return;

            SwitchSectionSyntax section = FindFixableSection(sections);

            if (section == null)
                return;

            context.ReportDiagnostic(
                DiagnosticDescriptors.MergeSwitchSectionsWithEquivalentContent,
                Location.Create(switchStatement.SyntaxTree, section.Statements.Span));
        }

        private static SwitchSectionSyntax FindFixableSection(SyntaxList<SwitchSectionSyntax> sections)
        {
            SyntaxList<StatementSyntax> statements = GetStatementsOrDefault(sections[0]);

            int i = 1;

            while (i < sections.Count)
            {
                SyntaxList<StatementSyntax> nextStatements = GetStatementsOrDefault(sections[i]);

                if (AreEquivalent(statements, nextStatements)
                    && !sections[i - 1].SpanOrTrailingTriviaContainsDirectives()
                    && !sections[i].SpanOrLeadingTriviaContainsDirectives())
                {
                    return sections[i - 1];
                }

                statements = nextStatements;
                i++;
            }

            return null;
        }

        private static bool AreEquivalent(SyntaxList<StatementSyntax> statements1, SyntaxList<StatementSyntax> statements2)
        {
            int count = statements1.Count;

            if (count == 1)
            {
                return statements2.Count == 1
                    && AreEquivalent(statements1[0], statements2[0]);
            }
            else if (count == 2)
            {
                return statements2.Count == 2
                    && AreEquivalentJumpStatements(statements1[1], statements2[1])
                    && AreEquivalent(statements1[0], statements2[0]);
            }

            return false;
        }

        private static bool AreEquivalent(StatementSyntax statement1, StatementSyntax statement2)
        {
            return statement1.Kind() == statement2.Kind()
                && CSharpFactory.AreEquivalent(statement1, statement2)
                && statement1.DescendantTrivia().All(f => f.IsWhitespaceOrEndOfLineTrivia())
                && statement2.DescendantTrivia().All(f => f.IsWhitespaceOrEndOfLineTrivia());
        }

        private static bool AreEquivalentJumpStatements(StatementSyntax statement1, StatementSyntax statement2)
        {
            switch (statement1)
            {
                case BreakStatementSyntax breakStatement:
                    {
                        return statement2.Kind() == SyntaxKind.BreakStatement;
                    }
                case ReturnStatementSyntax returnStatement:
                    {
                        return returnStatement.Expression == null
                            && (statement2 is ReturnStatementSyntax returnStatement2)
                            && returnStatement2.Expression == null;
                    }
                case ThrowStatementSyntax throwStatement:
                    {
                        return throwStatement.Expression == null
                            && (statement2 is ThrowStatementSyntax throwStatement2)
                            && throwStatement2.Expression == null;
                    }
            }

            return false;
        }

        internal static SyntaxList<StatementSyntax> GetStatementsOrDefault(SwitchSectionSyntax section)
        {
            foreach (SwitchLabelSyntax label in section.Labels)
            {
                if (!label.Kind().Is(SyntaxKind.CaseSwitchLabel, SyntaxKind.DefaultSwitchLabel))
                    return default(SyntaxList<StatementSyntax>);
            }

            SyntaxList<StatementSyntax> statements = section.Statements;

            if (statements.Count == 1
                && (statements[0] is BlockSyntax block))
            {
                return block.Statements;
            }

            return statements;
        }
    }
}