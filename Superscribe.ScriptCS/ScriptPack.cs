﻿namespace Superscribe.ScriptCS
{
    using System.ComponentModel.Composition;
    using System.Linq;

    using ScriptCs.Contracts;

    public class ScriptPack : IScriptPack
    {
        [ImportingConstructor]
        public ScriptPack()
        {
        }

        IScriptPackContext IScriptPack.GetContext()
        {
            return new Superscribe();
        }

        void IScriptPack.Initialize(IScriptPackSession session)
        {
            //session.AddReference("Superscribe");
            var namespaces = new[]
                {
                    "Superscribe"
                }.ToList();

            namespaces.ForEach(session.ImportNamespace);
        }

        void IScriptPack.Terminate()
        {
        }
    }
}
