use [BarRestauranteDb]

USE [BarRestauranteDb]
GO

/****** Objeto: Table [dbo].[LogTracker] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[LogTracker](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[AppUserId] [bigint] NULL,
	[DirectoryName] [varchar](150) NULL, -- Diretório ou Camada de origem (ex: Controllers, Services, Repositories)
	[ClassName] [varchar](150) NOT NULL, -- Nome da Classe de origem
	[MethodName] [varchar](150) NOT NULL, -- Nome do Método de origem
	[IsSuccess] [bit] NOT NULL, -- 1 para Sucesso, 0 para Erro
	[ExecutionTimeMs] [bigint] NULL, -- Tempo de execução em milissegundos (para auditoria de performance/desperdício)
	[Message] [nvarchar](max) NULL, -- Mensagem descritiva ou payload resumido
	[ErrorMessage] [nvarchar](max) NULL, -- Mensagem de erro caso ocorra exceção
	[StackTrace] [nvarchar](max) NULL, -- Pilha de erro para auditoria detalhada
	[IpAddress] [varchar](45) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
	[IsActive] [bit] NOT NULL,
 CONSTRAINT [PK_LogTracker] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

-- Valores padrão (Constraints)
ALTER TABLE [dbo].[LogTracker] ADD  CONSTRAINT [DF_LogTracker_CreatedAt]  DEFAULT (sysdatetime()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[LogTracker] ADD  CONSTRAINT [DF_LogTracker_IsActive]  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[LogTracker] ADD  CONSTRAINT [DF_LogTracker_IsSuccess]  DEFAULT ((1)) FOR [IsSuccess]
GO

-- Chave Estrangeira opcional com AppUser para rastrear quem executou a ação
ALTER TABLE [dbo].[LogTracker] WITH CHECK ADD  CONSTRAINT [FK_LogTracker_AppUser] FOREIGN KEY([AppUserId])
REFERENCES [dbo].[AppUser] ([Id])
GO
ALTER TABLE [dbo].[LogTracker] CHECK CONSTRAINT [FK_LogTracker_AppUser]
GO
