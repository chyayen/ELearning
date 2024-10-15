using PagedList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ELearning.Models
{
    public class AdminViewModel
    {
    }
     

    public class ResultModel
    {
        public bool success { get; set; } = false;
        public string message { get; set; } = "";
        public int id { get; set; } = 0;
    }

    public class ResultQuestionUpdateModel
    {
        public bool success { get; set; } = false;
        public string message { get; set; } = "";
        public QuestionModel question { get; set; } = new QuestionModel();
    }
}