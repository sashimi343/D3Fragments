using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BasicWebApp.Domain.Books;

public enum BookStatus
{
    [Display(Name = "On shelf")]
    Available,

    [Display(Name = "On loan")]
    OnLoan,

    [Display(Name = "On hold")]
    OnHold
}
