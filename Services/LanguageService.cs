using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Linq;

namespace NexusLauncher.Services;

public enum AppLanguage { Portuguese_BR, English, Espanol }

public enum AppTheme { Dark, Light }

public partial class LanguageService : ObservableObject
{
    private static readonly LanguageService _instance = new();
    public static LanguageService Instance => _instance;

    [ObservableProperty]
    private AppLanguage current = AppLanguage.Portuguese_BR;

    public string CurrentCode => Current switch
    {
        AppLanguage.Portuguese_BR => "pt-BR",
        AppLanguage.English => "en",
        AppLanguage.Espanol => "es",
        _ => "pt-BR"
    };

    public string T(string key)
    {
        return Current switch
        {
            AppLanguage.English => EnglishDict.GetValueOrDefault(key, key),
            AppLanguage.Espanol => EspanolDict.GetValueOrDefault(key, key),
            _ => PortugueseDict.GetValueOrDefault(key, key)
        };
    }

    public void ApplyByCode(string code)
    {
        Current = code switch
        {
            "en" => AppLanguage.English,
            "es" => AppLanguage.Espanol,
            _ => AppLanguage.Portuguese_BR
        };
    }

    private readonly System.Collections.Generic.Dictionary<string, string> PortugueseDict = new()
    {
        { "play.title", "Jogar" },
        { "play.subtitle", "Selecione uma versão e inicie o Minecraft." },
        { "play.button.play", "JOGAR" },
        { "play.button.install", "INSTALAR" },
        { "play.button.install_java", "INSTALAR JAVA" },
        { "play.player_account", "Conta do jogador" },
        { "play.version_label", "Versão:" },
        { "play.downloads", "Downloads" },
        { "instances.title", "Minhas Instâncias" },
        { "instances.subtitle", "Gerencie seus perfis Minecraft e modpacks instalados." },
        { "instances.search.placeholder", "Pesquisar instâncias..." },
        { "instances.button.new", "Nova" },
        { "instances.action.play", "Jogar" },
        { "instances.action.remove", "Remover" },
        { "instances.empty", "Nenhuma instância encontrada." },
        { "instances.empty_action", "Crie sua primeira instância" },
        { "mods.title", "NexusMods" },
        { "mods.subtitle", "Explore mods, modpacks e mais do Modrinth e CurseForge." },
        { "mods.search.placeholder", "Pesquisar..." },
        { "mods.version.placeholder", "1.20.1" },
        { "mods.button.search", "Buscar" },
        { "mods.button.install", "Instalar" },
        { "mods.status.loading", "Buscando projetos..." },
        { "mods.status.loading_more", "Carregando mais..." },
        { "mods.status.empty", "Nenhum resultado ainda." },
        { "mods.button.load_more", "Carregar mais" },
        { "settings.title", "Configurações" },
        { "settings.subtitle", "Ajuste o launcher ao seu gosto." },
        { "settings.section.performance", "DESEMPENHO" },
        { "settings.section.java", "JAVA" },
        { "settings.section.directories", "DIRETÓRIOS" },
        { "settings.section.window", "JANELA DO JOGO" },
        { "settings.section.behavior", "COMPORTAMENTO" },
        { "settings.section.appearance", "APARÊNCIA" },
        { "settings.section.versions", "CATEGORIAS" },
        { "settings.subsection.category_filters", "Selecione as categorias exibidas na tela Jogar" },
        { "settings.button.save", "Salvar" },
        { "settings.button.restore", "Restaurar padrões" },
        { "settings.button.detect", "Detectar" },
        { "settings.button.open_folder", "Abrir pasta" },
        { "settings.status.saved", "✓ Configurações salvas" },
        { "settings.status.loaded", "Pronto" },
        { "settings.status.restored", "↺ Padrões restaurados" },
        { "settings.status.java_detected", "✓ Java detectado" },
        { "settings.status.java_not_detected", "✕ Java não encontrado" },
        { "dialog.create.title", "Nova Instância" },
        { "dialog.create.subtitle", "Configure os detalhes do seu novo perfil." },
        { "dialog.create.name_label", "Nome da Instância" },
        { "dialog.create.name_placeholder", "Ex: Survival 1.20" },
        { "dialog.create.version_label", "Versão do Minecraft" },
        { "dialog.create.loader_label", "Loader" },
        { "dialog.create.button.cancel", "Cancelar" },
        { "dialog.create.button.create", "Criar" },
        { "install.select_instance.title", "Instalar em instância" },
        { "install.select_instance.subtitle", "Selecione a instância para instalar o conteúdo" },
        { "install.select_instance.no_instances", "Nenhuma instância encontrada. Crie uma primeiro." },
        { "install.button.install", "Instalar" },
        { "install.button.cancel", "Cancelar" }
    };

    private readonly System.Collections.Generic.Dictionary<string, string> EnglishDict = new()
    {
        { "play.title", "Play" },
        { "play.subtitle", "Choose your version and launch Minecraft." },
        { "play.button.play", "PLAY" },
        { "play.button.install", "INSTALL" },
        { "play.button.install_java", "INSTALL JAVA" },
        { "play.player_account", "Player Account" },
        { "play.version_label", "Version:" },
        { "play.downloads", "Downloads" },
        { "instances.title", "My Instances" },
        { "instances.subtitle", "Manage your Minecraft profiles and modpacks." },
        { "instances.search.placeholder", "Search instances..." },
        { "instances.button.new", "New" },
        { "instances.action.play", "Play" },
        { "instances.action.remove", "Remove" },
        { "instances.empty", "No instances found." },
        { "instances.empty_action", "Create your first instance" },
        { "mods.title", "NexusMods" },
        { "mods.subtitle", "Browse mods, modpacks and more from Modrinth and CurseForge." },
        { "mods.search.placeholder", "Search..." },
        { "mods.version.placeholder", "1.20.1" },
        { "mods.button.search", "Search" },
        { "mods.button.install", "Install" },
        { "mods.status.loading", "Searching projects..." },
        { "mods.status.loading_more", "Loading more..." },
        { "mods.status.empty", "No results yet." },
        { "mods.button.load_more", "Load more" },
        { "settings.title", "Settings" },
        { "settings.subtitle", "Tailor the launcher to your style." },
        { "settings.section.performance", "PERFORMANCE" },
        { "settings.section.java", "JAVA" },
        { "settings.section.directories", "DIRECTORIES" },
        { "settings.section.window", "GAME WINDOW" },
        { "settings.section.behavior", "BEHAVIOR" },
        { "settings.section.appearance", "APPEARANCE" },
        { "settings.section.versions", "CATEGORIES" },
        { "settings.subsection.category_filters", "Select categories shown in the Play screen" },
        { "settings.button.save", "Save" },
        { "settings.button.restore", "Restore defaults" },
        { "settings.button.detect", "Detect" },
        { "settings.button.open_folder", "Open folder" },
        { "settings.status.saved", "✓ Settings saved" },
        { "settings.status.loaded", "Ready" },
        { "settings.status.restored", "↺ Defaults restored" },
        { "settings.status.java_detected", "✓ Java detected" },
        { "settings.status.java_not_detected", "✕ Java not found" },
        { "dialog.create.title", "New Instance" },
        { "dialog.create.subtitle", "Configure your new profile details." },
        { "dialog.create.name_label", "Instance Name" },
        { "dialog.create.name_placeholder", "E.g. Survival 1.20" },
        { "dialog.create.version_label", "Minecraft Version" },
        { "dialog.create.loader_label", "Loader" },
        { "dialog.create.button.cancel", "Cancel" },
        { "dialog.create.button.create", "Create" },
        { "install.select_instance.title", "Install to instance" },
        { "install.select_instance.subtitle", "Select the instance to install the content" },
        { "install.select_instance.no_instances", "No instances found. Create one first." },
        { "install.button.install", "Install" },
        { "install.button.cancel", "Cancel" }
    };

    private readonly System.Collections.Generic.Dictionary<string, string> EspanolDict = new()
    {
        { "play.title", "Jugar" },
        { "play.subtitle", "Elige tu versión e inicia Minecraft." },
        { "play.button.play", "JUGAR" },
        { "play.button.install", "INSTALAR" },
        { "play.button.install_java", "INSTALAR JAVA" },
        { "play.player_account", "Cuenta del jugador" },
        { "play.version_label", "Versión:" },
        { "play.downloads", "Descargas" },
        { "instances.title", "Mis Instancias" },
        { "instances.subtitle", "Administra tus perfiles y modpacks de Minecraft." },
        { "instances.search.placeholder", "Buscar instancias..." },
        { "instances.button.new", "Nueva" },
        { "instances.action.play", "Jugar" },
        { "instances.action.remove", "Eliminar" },
        { "instances.empty", "No se encontraron instancias." },
        { "instances.empty_action", "Crea tu primera instancia" },
        { "mods.title", "NexusMods" },
        { "mods.subtitle", "Explora mods, modpacks y más de Modrinth y CurseForge." },
        { "mods.search.placeholder", "Buscar..." },
        { "mods.version.placeholder", "1.20.1" },
        { "mods.button.search", "Buscar" },
        { "mods.button.install", "Instalar" },
        { "mods.status.loading", "Buscando proyectos..." },
        { "mods.status.loading_more", "Cargando más..." },
        { "mods.status.empty", "Aún no hay resultados." },
        { "mods.button.load_more", "Cargar más" },
        { "settings.title", "Configuración" },
        { "settings.subtitle", "Ajusta el launcher a tu estilo." },
        { "settings.section.performance", "RENDIMIENTO" },
        { "settings.section.java", "JAVA" },
        { "settings.section.directories", "DIRECTORIOS" },
        { "settings.section.window", "VENTANA DEL JUEGO" },
        { "settings.section.behavior", "COMPORTAMIENTO" },
        { "settings.section.appearance", "APARIENCIA" },
        { "settings.section.versions", "CATEGORÍAS" },
        { "settings.subsection.category_filters", "Selecciona las categorías mostradas en Jugar" },
        { "settings.button.save", "Guardar" },
        { "settings.button.restore", "Restaurar valores" },
        { "settings.button.detect", "Detectar" },
        { "settings.button.open_folder", "Abrir carpeta" },
        { "settings.status.saved", "✓ Configuración guardada" },
        { "settings.status.loaded", "Listo" },
        { "settings.status.restored", "↺ Valores restaurados" },
        { "settings.status.java_detected", "✓ Java detectado" },
        { "settings.status.java_not_detected", "✕ Java no encontrado" },
        { "dialog.create.title", "Nueva Instancia" },
        { "dialog.create.subtitle", "Configura los detalles de tu nuevo perfil." },
        { "dialog.create.name_label", "Nombre de la Instancia" },
        { "dialog.create.name_placeholder", "Ej.: Supervivencia 1.20" },
        { "dialog.create.version_label", "Versión de Minecraft" },
        { "dialog.create.loader_label", "Cargador" },
        { "dialog.create.button.cancel", "Cancelar" },
        { "dialog.create.button.create", "Crear" },
        { "install.select_instance.title", "Instalar en instancia" },
        { "install.select_instance.subtitle", "Selecciona la instancia para instalar el contenido" },
        { "install.select_instance.no_instances", "No se encontraron instancias. Crea una primero." },
        { "install.button.install", "Instalar" },
        { "install.button.cancel", "Cancelar" }
    };
}