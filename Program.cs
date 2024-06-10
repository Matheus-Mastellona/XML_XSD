using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

class Program
{
    static void Main(string[] args)
    {
        string xmlFilePath = @"C:\Users\Usuario\Downloads\Compiladores\arquivo.xml";
        string xmlContent = LoadXmlFile(xmlFilePath);

        if (HasSingleTag(xmlContent))
        {
            bool allTagsClosed = CheckAllTagsClosed(xmlContent);
            Console.WriteLine("Todas as tags abertas foram fechadas corretamente: " + allTagsClosed);

            if (allTagsClosed)
            {
                string content = RemoveXmlTags(xmlContent);
                Console.WriteLine(content);
            }
            else
            {
                Console.WriteLine("Há tags abertas que não foram fechadas corretamente. Não é possível remover as tags XML.");
            }
        }
        else
        {
            Console.WriteLine("O XML deve conter apenas uma única tag principal.");
        }

        // Verificar se há uma referência a um arquivo XSD nas tags do XML
        if (HasXsdReferenceInTags(xmlContent))
        {
            Console.WriteLine("Referência a um arquivo XSD encontrado nas tags do XML.");

            // Carregar e validar o arquivo XSD
            string xsdFilePath = @"C:\Users\Usuario\Downloads\Compiladores\arquivo.xsd";
            string xsdContent = LoadXsdFile(xsdFilePath);

            if (xsdContent != null)
            {
                if (HasSingleTag(xsdContent))
                {
                    bool allXsdTagsClosed = CheckAllTagsClosed(xsdContent);
                    Console.WriteLine("Todas as tags abertas no XSD foram fechadas corretamente: " + allXsdTagsClosed);

                    if (!allXsdTagsClosed)
                    {
                        Console.WriteLine("Há tags abertas no XSD que não foram fechadas corretamente.");
                    }
                    else
                    {
                        bool attributesValid = ValidateAttributesInTags(xsdContent);
                        Console.WriteLine("Os atributos 'name' e 'type' estão presentes onde necessário: " + attributesValid);

                        if (attributesValid)
                        {
                            bool xmlValid = ValidarXMLComXSD(xmlContent, xsdContent);
                            Console.WriteLine("O XML é válido em relação ao XSD: " + xmlValid);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("O XSD deve conter apenas uma única tag principal.");
                }
            }
        }
        else
        {
            Console.WriteLine("Nenhuma referência a um arquivo XSD encontrado nas tags do XML.");
        }
    }

    static bool HasSingleTag(string xmlContent)
    {
        // Encontrando a primeira tag de abertura, ignorando a instrução de processamento
        Match match = Regex.Match(xmlContent, @"<(?!.*\?)(?<tag>[^/].*?)>");

        // Verificando se a primeira tag é a única tag principal
        if (match.Success)
        {
            string firstTagName = match.Groups["tag"].Value;
            string xmlWithoutFirstTag = xmlContent.Substring(match.Index + match.Length);

            // Verificando se não há outra tag de abertura antes da próxima tag de fechamento
            return !Regex.IsMatch(xmlWithoutFirstTag, $"<{firstTagName}[^/]*?>");
        }

        return false;
    }

    static bool CheckAllTagsClosed(string xmlContent)
    {
        // Removendo a instrução de processamento
        xmlContent = Regex.Replace(xmlContent, "<\\?.*?\\?>", "");

        // Encontrando todas as tags, incluindo tags auto-fechadas
        MatchCollection tags = Regex.Matches(xmlContent, @"<[^>]+>");

        int openTagCount = 0;
        foreach (Match tag in tags)
        {
            string tagValue = tag.Value;

            // Ignorar tags auto-fechadas
            if (tagValue.EndsWith("/>"))
            {
                continue;
            }

            if (tagValue.StartsWith("</"))
            {
                openTagCount--;
            }
            else
            {
                openTagCount++;
            }
        }

        Console.WriteLine($"Total tags (excluding self-closing): {tags.Count}");
        Console.WriteLine($"Total opening and closing tags balance: {openTagCount}");

        return openTagCount == 0;
    }

    static string RemoveXmlTags(string xmlContent)
    {
        try
        {
            // Expressão regular para remover as tags XML
            string noTagsContent = Regex.Replace(xmlContent, "<.*?>", String.Empty);
            return noTagsContent;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Ocorreu um erro: " + ex.Message);
            return null;
        }
    }

    static bool HasXsdReferenceInTags(string xmlContent)
    {
        // Verificando se há uma referência a um arquivo XSD nas tags do XML
        return Regex.IsMatch(xmlContent, @"\bxmlns:[^=\s]+=""[^""]*""");
    }

    static string LoadXmlFile(string filePath)
    {
        try
        {
            return File.ReadAllText(filePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Ocorreu um erro ao carregar o arquivo XML: " + ex.Message);
            return null;
        }
    }

    static string LoadXsdFile(string filePath)
    {
        try
        {
            return File.ReadAllText(filePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Ocorreu um erro ao carregar o arquivo XSD: " + ex.Message);
            return null;
        }
    }

    static bool ValidateAttributesInTags(string xmlContent)
    {
        // Encontrar todas as tags de elementos
        MatchCollection elementTags = Regex.Matches(xmlContent, @"<xs:element[^>]*>");

        foreach (Match elementTag in elementTags)
        {
            string tagContent = elementTag.Value;
            bool hasName = Regex.IsMatch(tagContent, @"\bname\s*=\s*""[^""]*""");

            // Verificar se a tag é seguida por <xs:complexType> e <xs:sequence>
            string pattern = $@"{Regex.Escape(tagContent)}\s*<xs:complexType>\s*<xs:sequence>";
            bool isComplex = Regex.IsMatch(xmlContent, pattern, RegexOptions.Singleline);

            // Tags simples devem ter ambos os atributos, tags complexas podem omitir o type
            if (hasName && (isComplex || Regex.IsMatch(tagContent, @"\btype\s*=\s*""[^""]*""")))
            {
                continue;
            }

            // Se não, a tag está incompleta
            Console.WriteLine($"Tag incompleta encontrada: {tagContent}");
            return false;
        }

        return true;
    }

    static Dictionary<string, List<string>> ParseXSD(string xsd)
    {
        var xsdElements = new Dictionary<string, List<string>>();
        Regex elementPattern = new Regex(@"<xs:element name=""(\w+)""(.*?)>");
        MatchCollection matches = elementPattern.Matches(xsd);

        foreach (Match match in matches)
        {
            string elementName = match.Groups[1].Value;
            string elementAttributes = match.Groups[2].Value;
            var attributesList = new List<string>();

            // Extract attributes (simplified)
            Regex attributePattern = new Regex(@"(\w+)=""(\w+)""");
            MatchCollection attributeMatches = attributePattern.Matches(elementAttributes);

            foreach (Match attrMatch in attributeMatches)
            {
                attributesList.Add(attrMatch.Groups[1].Value);
            }

            xsdElements[elementName] = attributesList;
        }

        return xsdElements;
    }

    static bool ValidarXMLComXSD(string xml, string xsd)
    {
        var xsdElements = ParseXSD(xsd);

        // Basic XML validation against XSD
        Regex tagPattern = new Regex(@"<(\w+)(.*?)>");
        MatchCollection matches = tagPattern.Matches(xml);

        foreach (Match match in matches)
        {
            string tagName = match.Groups[1].Value;
            string tagAttributes = match.Groups[2].Value;

            if (!xsdElements.ContainsKey(tagName))
            {
                Console.WriteLine($"Elemento {tagName} não encontrado no XSD.");
                return false;
            }

            // Validate attributes (simplified)
            Regex attributePattern = new Regex(@"(\w+)=""(\w+)""");
            MatchCollection attributeMatches = attributePattern.Matches(tagAttributes);

            foreach (Match attrMatch in attributeMatches)
            {
                string attrName = attrMatch.Groups[1].Value;
                if (!xsdElements[tagName].Contains(attrName))
                {
                    Console.WriteLine($"Atributo {attrName} do elemento {tagName} não encontrado no XSD.");
                    return false;
                }
            }
        }

        return true;
    }
}
