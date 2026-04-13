global using System.Collections.Generic;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;

global using AutoFixture;
global using AutoFixture.AutoMoq;
global using Collectibles.Application.Interfaces;
global using Collectibles.Domain.Common.Entities;
global using Collectibles.Domain.Common.Enums;
global using Collectibles.Domain.Entities;
global using Collectibles.Infrastructure.Persistence;
global using FluentAssertions;
global using MediatR;
global using Microsoft.EntityFrameworkCore;
global using Moq;
global using Xunit;
