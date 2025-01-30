using AutoFixture;
using AutoFixture.NUnit4;
using CsvHelper;
using CsvHelper.Configuration;
using RaggedBooks.Core.TextExtraction;
using System.Globalization;


namespace RaggedBooks.Tests
{
    internal static class CsvDataLoader
    {
        public static List<Chapter> GetChaptersFromCsv()
        {
            var csvFilePath = string.Concat(
                Environment.CurrentDirectory,
                Path.DirectorySeparatorChar,
                "data",
                Path.DirectorySeparatorChar,
                "chapterpaths.csv");


            using var reader = new StreamReader(csvFilePath);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));
            return csv.GetRecords<Chapter>().ToList();
        }
    }

    internal class CsvAutoDataAttribute : AutoDataAttribute
    {
        public CsvAutoDataAttribute()
            : base(() => CreateFixtureWithCsvData())
        {
        }

        private static IFixture CreateFixtureWithCsvData()
        {
            var fixture = new Fixture();

            // Load CSV data
            var chapters = CsvDataLoader.GetChaptersFromCsv();

            // Customize the fixture to provide the CSV data
            fixture.Register(() => chapters);

            return fixture;
        }
    }


    public class ChapterPathTests
    {
        [Test]
        [CsvAutoData()]
        public void Returns_Chapter_Paths(List<Chapter> chapters)
        {
            var sut = new ChapterPath(chapters);

            //Todo
        }

        [Test]
        public void Returns_Correct_ChapterPath()
        {
            var chapters = new List<Chapter>
            {
                new Chapter("Chapter 1", 0, 1),
                new Chapter("Foreword", 1, 3),
                new Chapter("Author", 1, 5),
                new Chapter("Acknowledgements", 2, 9)
            };

            var sut = new ChapterPath(chapters);

            var path = sut.ByPageNumber(10);
            Assert.That(path.Equals("Chapter 1 > Author > Acknowledgements"));

            path = sut.ByPageNumber(5);
            Assert.That(path.Equals("Chapter 1 > Author"));
        }
    }
}