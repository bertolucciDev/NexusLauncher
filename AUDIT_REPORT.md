# NexusLauncher Audit Report

## Escopo da auditoria

Foram auditadas as camadas de UI, ViewModels, Models, Services, Storage e integração Minecraft. A revisão também considerou a documentação oficial atual da CmlLib e da API Modrinth antes de introduzir mudanças relacionadas a essas integrações.

## Bugs encontrados

- O username era salvo apenas durante a ação principal de jogar; alterações feitas no campo podiam ser perdidas se o usuário fechasse o launcher antes de iniciar o jogo.
- O progresso de download era artificial em parte do fluxo e não tinha cálculo real de velocidade, bytes baixados, tamanho total e ETA.
- Após instalação, a versão selecionada podia não refletir imediatamente o estado instalado, mantendo o botão como `INSTALAR` em alguns casos.
- A detecção de versões modificadas dependia de heurísticas simples e agrupava muitos loaders em uma categoria genérica.
- A aba Downloads não correspondia ao requisito atual de loja integrada Modrinth.
- A detecção de Java considerava apenas `JAVA_HOME`/`PATH` e aceitava apenas Java 17+, sem modelar versões 8, 17, 21 e futuras.
- A UI continha textos de status pouco úteis e categorias incompletas para versões.

## Correções realizadas

- Persistência automática do nickname quando o campo muda, usando o armazenamento local existente.
- Ampliação do modelo de configurações para salvar última categoria, Java, tema e preferência de segundo plano.
- Criação de um modelo de progresso de download com bytes baixados, total, percentual, velocidade média móvel e ETA.
- Reescrita do `DownloadService` para streaming com `HttpCompletionOption.ResponseHeadersRead` e média móvel sobre janela recente de amostras.
- Atualização do fluxo de jogar/instalar para receber progresso, atualizar estado instalado e recarregar a lista sem reiniciar.
- Reestruturação das categorias de versões para: Releases, Snapshots, Forge, NeoForge, Fabric, Quilt, OptiFine, LiteLoader, Modpacks e Custom.
- Detecção de versões locais usando sinais de `version.json`, `inheritsFrom`, libraries, chaves/valores JSON, loaders e marcadores de modpacks.
- Detecção de modpacks por `manifest.json`, `modrinth.index.json`, `instance.cfg` e `mmc-pack.json`.
- Substituição da aba Downloads por Modrinth com pesquisa, tipo de projeto, loader, versão Minecraft, ordenação e cards de resultado.
- Criação de `ModrinthService` usando a API v2 oficial, endpoint `/search`, facets e `User-Agent` dedicado.
- Melhoria da detecção de Java para encontrar instalações em `JAVA_HOME`, `PATH` e diretórios comuns do sistema, com parsing de versões 8/17/21/24.
- Lançamento do Minecraft preservado com `UseShellExecute=false`, `CreateNoWindow=true` e redirecionamento de saída para evitar janelas CMD extras.

## Arquivos modificados

- `Models/LauncherSettings.cs`
- `Models/MinecraftVersionCategory.cs`
- `Models/MinecraftVersionInfo.cs`
- `Minecraft/Java/JavaManager.cs`
- `Minecraft/MinecraftService.cs`
- `Minecraft/Versions/VersionManager.cs`
- `Services/DownloadService.cs`
- `Services/JavaService.cs`
- `Services/SettingsService.cs`
- `Services/VersionService.cs`
- `ViewModels/ModsViewModel.cs`
- `ViewModels/PlayViewModel.cs`
- `ViewModels/VersionsViewModel.cs`
- `Views/MainWindow.axaml`
- `Views/ModsView.axaml`
- `Views/VersionsView.axaml`

## Arquivos criados

- `Models/DownloadProgressInfo.cs`
- `Models/ModrinthProject.cs`
- `Services/ModrinthService.cs`
- `AUDIT_REPORT.md`

## Arquivos removidos

- Nenhum arquivo foi removido nesta etapa. A pasta `tmp/` permanece excluída da compilação pelo `.csproj`; recomenda-se removê-la em sprint dedicada caso não seja mais usada para investigação.

## Melhorias de arquitetura

- Separação explícita entre progresso de download (`DownloadProgressInfo`) e serviços consumidores.
- Introdução de um serviço dedicado para Modrinth, evitando lógica de HTTP dentro do ViewModel.
- Categorias de versões agora são expressivas e alinhadas ao domínio do launcher.
- Java passou a ser modelado como instalação detectável (`JavaInstallation`) com versão principal.

## Melhorias de UX/UI

- A navegação principal agora aponta para Modrinth em vez de Downloads.
- A página de versões exibe todas as categorias requeridas.
- A página Jogar mostra feedback de download com percentual, velocidade e ETA.
- Textos genéricos de status foram reduzidos e substituídos por feedback contextual.

## Melhorias de performance

- Downloads agora são feitos por streaming, evitando carregar arquivos inteiros em memória.
- A velocidade usa média móvel em janela curta, reduzindo oscilações e corrigindo ETA instável.
- Consultas de versões instaladas são agrupadas em memória no ViewModel antes da aplicação de filtros.

## CmlLib

- A documentação antiga indicada redireciona para a documentação mais nova. A revisão confirmou que a CmlLib oferece instalação vanilla, instalação de Java, loaders, eventos/progresso de download, opções de launch e construção de processo.
- O projeto já usa `MinecraftLauncher.InstallAsync` e `BuildProcessAsync`, que continuam adequados para o fluxo atual.
- Pendente: integrar eventos nativos de progresso da CmlLib quando a API exata estiver disponível no pacote instalado no ambiente de build. Como o CLI .NET não está disponível neste container, a validação por IntelliSense/compilação local ficou pendente.

## Modrinth

- A integração usa a API oficial v2 em `https://api.modrinth.com/v2/`.
- A busca usa `/search` com facets para `project_type`, `categories` e `versions`, além dos índices documentados (`relevance`, `downloads`, `follows`, `updated`, `newest`).
- A documentação exige um `User-Agent` identificável; o serviço configura `NexusLauncher/0.1 (desktop launcher)`.
- Pendente: instalação real de projetos Modrinth, resolução de versões/arquivos e descompactação de `.mrpack`.

## Pendências futuras

- Implementar fila real com pausar, continuar, cancelar e downloads simultâneos.
- Implementar instalação real de modpacks Modrinth com leitura de `modrinth.index.json`.
- Permitir escolher a instalação destino ao instalar mods individuais.
- Adicionar cache de resultados Modrinth e imagens.
- Baixar Java automaticamente quando a instalação compatível não existir.
- Remover ou consolidar wrappers duplicados entre `Services/MinecraftService.cs` e `Minecraft/MinecraftService.cs`.
- Criar testes unitários para detecção de versões com fixtures de Forge, Fabric, Quilt, NeoForge, OptiFine, LiteLoader e modpacks.

## Sugestões para próxima sprint

1. Instalar o SDK .NET no ambiente CI/local e validar build Avalonia.
2. Criar testes automatizados para `VersionService`, `JavaManager` e `DownloadService`.
3. Implementar `DownloadManagerService` com fila persistente e controles de pausa/cancelamento.
4. Completar instalação Modrinth (`/project/{id}/version`, hashes, dependências e `.mrpack`).
5. Consolidar serviços duplicados e padronizar injeção de dependências.
