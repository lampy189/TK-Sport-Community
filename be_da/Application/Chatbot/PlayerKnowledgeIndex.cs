using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace be_da.Application.Chatbot;

// BM25 index of approved, public SQL policy chunks. Rebuilt per question to honor changes/revocation.
public sealed class PlayerKnowledgeIndex
{
    private readonly KnowledgeArticle[] articles;
    private readonly Dictionary<string, int>[] terms;
    private readonly Dictionary<string, int> documentFrequency = new();
    private readonly double averageLength;
    private static readonly HashSet<string> StopWords = new(
        "toi ban minh cho la va cua co khong the nao lam duoc thi voi ve mot nhung cac de khi trong tai sao gi bao nhieu xin hoi hay muon can".Split(' '));

    public PlayerKnowledgeIndex(IEnumerable<KnowledgeArticle> articles)
    {
        this.articles = articles.ToArray();
        terms = this.articles.Select(a => Tokens(a.Title + " " + a.Keywords + " " + a.Content)
            .GroupBy(t => t).ToDictionary(g => g.Key, g => g.Count())).ToArray();
        foreach (var document in terms)
            foreach (var term in document.Keys)
                documentFrequency[term] = documentFrequency.GetValueOrDefault(term) + 1;
        averageLength = Math.Max(1, terms.Length == 0 ? 0 : terms.Average(t => t.Values.Sum()));
    }

    public IReadOnlyList<KnowledgeArticle> Search(string question)
    {
        var query = Tokens(question).Distinct().ToArray();
        if (query.Length == 0) return [];
        return articles.Select((article, i) =>
        {
            var document = terms[i];
            var length = document.Values.Sum();
            var matched = query.Count(document.ContainsKey);
            var score = query.Sum(term =>
            {
                var frequency = document.GetValueOrDefault(term);
                if (frequency == 0) return 0;
                var idf = Math.Log(1 + (articles.Length - documentFrequency[term] + .5) / (documentFrequency[term] + .5));
                return idf * frequency * 2.2 / (frequency + 1.2 * (.25 + .75 * length / averageLength));
            });
            // Require multiple meaningful matches, or one exact distinctive query term.
            var relevant = matched >= Math.Min(2, query.Length) && score >= 1.0;
            return (article, score, relevant);
        }).Where(x => x.relevant).OrderByDescending(x => x.score).Take(3).Select(x => x.article).ToArray();
    }

    private static IEnumerable<string> Tokens(string value)
    {
        var normalized = new string(value.ToLowerInvariant().Replace('đ', 'd').Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
        var words = Regex.Matches(normalized, "[a-z0-9]+").Select(m => m.Value).ToArray();
        foreach (var word in words)
            if (word.Length > 1 && !StopWords.Contains(word)) yield return word;
        // Adjacent syllables improve Vietnamese phrase matching without an external tokenizer.
        for (var i = 0; i + 1 < words.Length; i++)
            if (!StopWords.Contains(words[i]) && !StopWords.Contains(words[i + 1]))
                yield return words[i] + "_" + words[i + 1];
    }
}
