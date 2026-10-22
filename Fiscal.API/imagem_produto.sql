START TRANSACTION;

CREATE TABLE produtos_imagens (
    "Id" uuid NOT NULL,
    "ProdutoId" uuid NOT NULL,
    "Imagem" bytea NOT NULL,
    "ContentType" character varying(50) NOT NULL,
    "TamanhoBytes" integer NOT NULL,
    "CriadoEm" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_produtos_imagens" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_produtos_imagens_produtos_ProdutoId" FOREIGN KEY ("ProdutoId") REFERENCES produtos ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_produtos_imagens_ProdutoId" ON produtos_imagens ("ProdutoId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261021190856_AdicionarImagemProduto', '8.0.31');

COMMIT;

