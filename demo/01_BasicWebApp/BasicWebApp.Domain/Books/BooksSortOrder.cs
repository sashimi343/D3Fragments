using System;
using System.Collections.Generic;

namespace BasicWebApp.Domain.Books;

public enum BooksSortOrder
{
    Default,
    IsbnAsc,
    IsbnDesc,
    TitleAsc,
    TitleDesc,
    AuthorAsc,
    AuthorDesc,
}
