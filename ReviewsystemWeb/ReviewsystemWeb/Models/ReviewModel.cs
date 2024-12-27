namespace ReviewsystemWeb.Models
{
    public class ReviewModel
    {
        public class Review
        {
            public int ReviewId { get; set; }
            public int Rating { get; set; }
            public DateTime CreatedAt { get; set; }
            public List<Question> Questions { get; set; } = new();
        }

        public class Question
        {
            public int QuestionId { get; set; }
            public string QuestionText { get; set; }
            public string ResponseText { get; set; }
            public int ReviewId { get; set; }
        }
    }
}
