namespace GenerateEntities
{
  using System;
  using System.Linq;
  using System.CodeDom;
  using Microsoft.Crm.Services.Utility;
  using System.Collections.Generic;
  using Microsoft.Xrm.Sdk.Metadata;
  using System.Globalization;
  using System.Text.RegularExpressions;

  public sealed class CodeCustomizationService : ICustomizeCodeDomService
  {
    public void CustomizeCodeDom(CodeCompileUnit codeUnit, IServiceProvider services)
    {
      var metadataProviderService = (IMetadataProviderService)services.GetService(typeof(IMetadataProviderService));
      IOrganizationMetadata metadata = metadataProviderService.LoadMetadata();

      foreach (CodeNamespace codeNamespace in codeUnit.Namespaces)
      {
        foreach (CodeTypeDeclaration codeTypeDeclaration in codeNamespace.Types)
        {
          if (codeTypeDeclaration.IsClass)
          {
            foreach (EntityMetadata entityMetadata in metadata.Entities.Where(e => e.SchemaName == codeTypeDeclaration.Name))
            {
              foreach (CodeTypeMember codeTypeMember in codeTypeDeclaration.Members)
              {
                if (codeTypeMember.GetType() == typeof(System.CodeDom.CodeMemberProperty) && codeTypeMember.CustomAttributes.Count > 0 && codeTypeMember.CustomAttributes[0].AttributeType.BaseType == "Microsoft.Xrm.Sdk.AttributeLogicalNameAttribute")
                {
                  AttributeMetadata attributeMetadata = entityMetadata.Attributes.Where(a => a.LogicalName == ((System.CodeDom.CodePrimitiveExpression)codeTypeMember.CustomAttributes[0].Arguments[0].Value).Value.ToString()).FirstOrDefault();

                  if (attributeMetadata != null)
                  {
                    string label = (attributeMetadata.DisplayName.UserLocalizedLabel == null ? attributeMetadata.LogicalName : (attributeMetadata.DisplayName.UserLocalizedLabel.Label ?? attributeMetadata.LogicalName));
                    string description = (attributeMetadata.Description.UserLocalizedLabel == null ? "None" : (attributeMetadata.Description.UserLocalizedLabel.Label ?? "None"));

                    codeTypeMember.CustomAttributes.Add(new CodeAttributeDeclaration { Name = "System.ComponentModel.Description", Arguments = { new CodeAttributeArgument { Value = new CodePrimitiveExpression(label) } } });

                    if (codeTypeMember.Comments != null)
                    {
                      for (int i = 0; i < codeTypeMember.Comments.Count - 1; i++)
                      {
                        if (codeTypeMember.Comments[i].Comment.Text.ToLowerInvariant() == "<summary>")
                        {
                          codeTypeMember.Comments[i + 1].Comment.Text = string.Format("{0}: {1}", label, description);
                        }
                      }
                    }
                  }
                }
              }
            }
          }
        }
      }
    }
  }
}
