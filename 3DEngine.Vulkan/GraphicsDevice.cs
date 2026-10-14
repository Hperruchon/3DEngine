// Copyright (c) Amer Koleci and Contributors.
// Licensed under the MIT License (MIT). See THIRD-PARTY-NOTICES.md, section 4.
// This file comes from the Vortice.Vulkan sample framework. TASK-0027 moved it
// into this project and changed it. ADR-0017 gives the rules for this project.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Vortice.Vulkan;
using static SDL3.SDL3;
using static Vortice.Vulkan.Vulkan;

namespace ThreeDEngine.Vulkan;

// The core of the device: the instance, the physical device, the device and the
// graphics queue (ADR-0025 item 2). With a window, it also holds the presentation to
// that window (Presenter). With no window, it makes no SDL call and needs no present
// support, so a runner with no display can draw into an OffscreenTarget. Until
// TASK-0076 the device took a window and always built the swapchain.
public unsafe sealed class GraphicsDevice : IDisposable
{
    private const string SwapchainExtension = "VK_KHR_swapchain";
    private const string DebugUtilsExtension = "VK_EXT_debug_utils";
    private const string SwapchainColorSpaceExtension = "VK_EXT_swapchain_colorspace";
    private const string ValidationFeaturesExtension = "VK_EXT_validation_features";

    // Each message of the validation layer since the start of the process. A test
    // fails when the count grows (ADR-0025 item 5).
    private static int s_validationMessages;

    public readonly bool DebugUtils;
    public readonly VkInstance VkInstance;

    // Vortice.Vulkan 3.x has no global function for an instance or a device.
    // Each table below holds the functions of one handle, and each function
    // takes that handle implicitly.
    public readonly VkInstanceApi InstanceApi;
    public readonly VkDeviceApi DeviceApi;

    private readonly VkDebugUtilsMessengerEXT _debugMessenger = VkDebugUtilsMessengerEXT.Null;
    public readonly VkPhysicalDevice PhysicalDevice;
    public readonly VkDevice VkDevice;
    public readonly uint GraphicsFamily;
    public readonly VkQueue GraphicsQueue;
    private readonly VkCommandPool _submitPool;

    // A device with no window: enableValidation turns the validation layer on, and
    // synchronizationValidation also turns on its checks of synchronization.
    public GraphicsDevice(string applicationName, bool enableValidation, Window? window, bool synchronizationValidation = false)
    {
        string[] availableInstanceLayers = EnumerateInstanceLayers();
        string[] availableInstanceExtensions = GetInstanceExtensions(null);

        List<string> instanceExtensions = [];
        if (window is not null)
            instanceExtensions.AddRange(SDL_Vulkan_GetInstanceExtensions());

        List<string> instanceLayers = enableValidation ? GetOptimalValidationLayers(availableInstanceLayers) : [];
        ValidationEnabled = instanceLayers.Count > 0;

        if (availableInstanceExtensions.Contains(DebugUtilsExtension))
        {
            DebugUtils = true;
            instanceExtensions.Add(DebugUtilsExtension);
        }

        if (window is not null && availableInstanceExtensions.Contains(SwapchainColorSpaceExtension))
            instanceExtensions.Add(SwapchainColorSpaceExtension);

        // The validation layer gives the extension of its features. Without it the
        // layer cannot turn on its checks of synchronization.
        var synchronization = synchronizationValidation && ValidationEnabled
            && instanceLayers.Any(layer => GetInstanceExtensions(layer).Contains(ValidationFeaturesExtension));
        if (synchronizationValidation && ValidationEnabled && !synchronization)
            throw new InvalidOperationException($"The validation layer does not give {ValidationFeaturesExtension}, so it cannot check the synchronization.");
        if (synchronization)
            instanceExtensions.Add(ValidationFeaturesExtension);
        SynchronizationValidationEnabled = synchronization;

        using var names = new NativeStrings([applicationName, "3DEngine"]);
        using var layerNames = new NativeStrings(instanceLayers);
        using var extensionNames = new NativeStrings(instanceExtensions);

        VkApplicationInfo appInfo = new()
        {
            pApplicationName = names.Pointers[0],
            applicationVersion = new VkVersion(1, 0, 0),
            pEngineName = names.Pointers[1],
            engineVersion = new VkVersion(1, 0, 0),
            apiVersion = VkVersion.Version_1_3
        };

        VkInstanceCreateInfo instanceCreateInfo = new()
        {
            pApplicationInfo = &appInfo,
            enabledLayerCount = layerNames.Count,
            ppEnabledLayerNames = layerNames.Pointers,
            enabledExtensionCount = extensionNames.Count,
            ppEnabledExtensionNames = extensionNames.Pointers
        };

        VkDebugUtilsMessengerCreateInfoEXT debugUtilsCreateInfo = new();
        VkValidationFeatureEnableEXT enabledFeature = VkValidationFeatureEnableEXT.SynchronizationValidation;
        VkValidationFeaturesEXT validationFeatures = new()
        {
            enabledValidationFeatureCount = 1,
            pEnabledValidationFeatures = &enabledFeature,
        };

        if (ValidationEnabled)
        {
            debugUtilsCreateInfo.messageSeverity = VkDebugUtilsMessageSeverityFlagsEXT.Error | VkDebugUtilsMessageSeverityFlagsEXT.Warning;
            debugUtilsCreateInfo.messageType = VkDebugUtilsMessageTypeFlagsEXT.Validation | VkDebugUtilsMessageTypeFlagsEXT.Performance;
            debugUtilsCreateInfo.pfnUserCallback = &DebugMessengerCallback;
            if (synchronization)
                debugUtilsCreateInfo.pNext = &validationFeatures;
            instanceCreateInfo.pNext = &debugUtilsCreateInfo;
        }

        VkResult result = vkCreateInstance(&instanceCreateInfo, null, out VkInstance);
        if (result != VkResult.Success)
        {
            throw new InvalidOperationException($"Failed to create vulkan instance: {result}");
        }

        InstanceApi = GetApi(VkInstance);

        if (ValidationEnabled)
        {
            InstanceApi.vkCreateDebugUtilsMessengerEXT(&debugUtilsCreateInfo, null, out _debugMessenger).CheckResult();
        }

        Log.Info($"Created VkInstance with version: {appInfo.apiVersion.Major}.{appInfo.apiVersion.Minor}.{appInfo.apiVersion.Patch}");
        foreach (var layer in instanceLayers)
        {
            Log.Info($"Instance layer '{layer}'");
        }

        foreach (var extension in instanceExtensions)
        {
            Log.Info($"Instance extension '{extension}'");
        }

        VkSurfaceKHR surface = window is null ? VkSurfaceKHR.Null : window.CreateSurface(VkInstance);

        // Find physical device, setup queue's and create device.
        uint physicalDevicesCount = 0;
        InstanceApi.vkEnumeratePhysicalDevices(&physicalDevicesCount, null).CheckResult();

        VkPhysicalDevice* physicalDevices = stackalloc VkPhysicalDevice[(int)Math.Max(1, physicalDevicesCount)];
        InstanceApi.vkEnumeratePhysicalDevices(&physicalDevicesCount, physicalDevices).CheckResult();

        for (int i = 0; i < physicalDevicesCount; i++)
        {
            VkPhysicalDevice physicalDevice = physicalDevices[i];

            if (IsDeviceSuitable(InstanceApi, physicalDevice, surface) == false)
                continue;

            InstanceApi.vkGetPhysicalDeviceProperties(physicalDevice, out VkPhysicalDeviceProperties checkProperties);
            bool discrete = checkProperties.deviceType == VkPhysicalDeviceType.DiscreteGpu;

            if (discrete || PhysicalDevice.IsNull)
            {
                PhysicalDevice = physicalDevice;
                if (discrete)
                {
                    // If this is discrete GPU, look no further (prioritize discrete GPU)
                    break;
                }
            }
        }

        // Until TASK-0076 the loop could end with no device, and the next call gave a
        // null handle to Vulkan; the process stopped in native code with no message
        // (R-0030, finding V5).
        if (PhysicalDevice.IsNull)
        {
            if (surface != VkSurfaceKHR.Null)
                InstanceApi.vkDestroySurfaceKHR(surface);
            Dispose();
            throw new InvalidOperationException(window is null
                ? $"No Vulkan device of the {physicalDevicesCount} found has a graphics queue."
                : $"No Vulkan device of the {physicalDevicesCount} found can draw and present to the window with {SwapchainExtension}.");
        }

        InstanceApi.vkGetPhysicalDeviceProperties(PhysicalDevice, out VkPhysicalDeviceProperties properties);
        Log.Info($"Vulkan device '{NativeStrings.Read(properties.deviceName)}'");

        var queueFamilies = FindQueueFamilies(InstanceApi, PhysicalDevice, surface);
        GraphicsFamily = queueFamilies.graphicsFamily;

        HashSet<uint> uniqueQueueFamilies = [queueFamilies.graphicsFamily];
        if (window is not null)
            uniqueQueueFamilies.Add(queueFamilies.presentFamily);

        float priority = 1.0f;
        uint queueCount = 0;
        VkDeviceQueueCreateInfo* queueCreateInfos = stackalloc VkDeviceQueueCreateInfo[2];

        foreach (uint queueFamily in uniqueQueueFamilies)
        {
            queueCreateInfos[queueCount++] = new VkDeviceQueueCreateInfo
            {
                queueFamilyIndex = queueFamily,
                queueCount = 1,
                pQueuePriorities = &priority
            };
        }

        using var deviceExtensionNames = new NativeStrings(window is null ? [] : [SwapchainExtension]);

        VkDeviceCreateInfo deviceCreateInfo = new()
        {
            queueCreateInfoCount = queueCount,
            pQueueCreateInfos = queueCreateInfos,
            enabledExtensionCount = deviceExtensionNames.Count,
            ppEnabledExtensionNames = deviceExtensionNames.Pointers,
            pEnabledFeatures = null,
        };

        result = InstanceApi.vkCreateDevice(PhysicalDevice, &deviceCreateInfo, null, out VkDevice);
        if (result != VkResult.Success)
            throw new Exception($"Failed to create Vulkan Logical Device, {result}");

        DeviceApi = GetApi(VkInstance, VkDevice);

        DeviceApi.vkGetDeviceQueue(queueFamilies.graphicsFamily, 0, out GraphicsQueue);

        VkCommandPoolCreateInfo submitPoolInfo = new()
        {
            flags = VkCommandPoolCreateFlags.Transient,
            queueFamilyIndex = GraphicsFamily,
        };
        DeviceApi.vkCreateCommandPool(&submitPoolInfo, null, out _submitPool).CheckResult();

        if (window is not null)
        {
            DeviceApi.vkGetDeviceQueue(queueFamilies.presentFamily, 0, out VkQueue presentQueue);
            Presenter = new Presenter(this, surface, window, presentQueue);
        }
    }

    // The presentation to the window, or null for a device with no window.
    public Presenter? Presenter { get; }

    // True when the validation layer is on.
    public bool ValidationEnabled { get; }

    // True when the validation layer also checks the synchronization.
    public bool SynchronizationValidationEnabled { get; }

    // The count of the messages of the validation layer in this process.
    public static int ValidationMessageCount => Volatile.Read(ref s_validationMessages);

    public void Dispose()
    {
        if (VkDevice.IsNotNull)
        {
            // Don't release anything until the GPU is completely idle.
            WaitIdle();

            Presenter?.Dispose();

            if (_submitPool != VkCommandPool.Null)
                DeviceApi.vkDestroyCommandPool(_submitPool);

            DeviceApi.vkDestroyDevice();
        }

        if (_debugMessenger != VkDebugUtilsMessengerEXT.Null)
        {
            InstanceApi.vkDestroyDebugUtilsMessengerEXT(_debugMessenger);
        }

        if (VkInstance != VkInstance.Null)
        {
            InstanceApi.vkDestroyInstance();
        }
    }

    // A shader module of one SPIR-V module. The caller destroys it after it creates
    // the pipelines that use it (TASK-0069; ADR-0017 said that R3 writes this
    // helper again).
    public VkShaderModule CreateShaderModule(byte[] spirv)
    {
        if (!ShaderLibrary.HasSpirvHeader(spirv))
            throw new ArgumentException("The bytes are not a SPIR-V module.", nameof(spirv));

        DeviceApi.vkCreateShaderModule((ReadOnlySpan<byte>)spirv, null, out VkShaderModule module).CheckResult();
        return module;
    }

    // The index of a memory type of this device that the bits permit and that has
    // each wanted property (MemoryTypes.Find).
    public uint FindMemoryType(uint permittedTypeBits, VkMemoryPropertyFlags wanted)
    {
        InstanceApi.vkGetPhysicalDeviceMemoryProperties(PhysicalDevice, out VkPhysicalDeviceMemoryProperties properties);
        var typeProperties = new VkMemoryPropertyFlags[properties.memoryTypeCount];
        for (var index = 0; index < typeProperties.Length; index++)
            typeProperties[index] = properties.memoryTypes[index].propertyFlags;

        var found = MemoryTypes.Find(permittedTypeBits, typeProperties, wanted);
        return found >= 0
            ? (uint)found
            : throw new InvalidOperationException($"No memory type of the device has the properties {wanted}.");
    }

    public void WaitIdle()
    {
        DeviceApi.vkDeviceWaitIdle().CheckResult();
    }

    // Records one command buffer, submits it to the graphics queue, and waits until
    // the device ends it (TASK-0076, for the offscreen target).
    public void Submit(Action<VkCommandBuffer> record)
    {
        DeviceApi.vkAllocateCommandBuffer(_submitPool, out VkCommandBuffer cmd).CheckResult();
        DeviceApi.vkCreateFence(VkFenceCreateFlags.None, out VkFence fence).CheckResult();
        try
        {
            VkCommandBufferBeginInfo beginInfo = new() { flags = VkCommandBufferUsageFlags.OneTimeSubmit };
            DeviceApi.vkBeginCommandBuffer(cmd, &beginInfo).CheckResult();
            record(cmd);
            DeviceApi.vkEndCommandBuffer(cmd).CheckResult();

            VkSubmitInfo submitInfo = new()
            {
                commandBufferCount = 1u,
                pCommandBuffers = &cmd,
            };
            DeviceApi.vkQueueSubmit(GraphicsQueue, submitInfo, fence).CheckResult();
            DeviceApi.vkWaitForFences(fence, true, ulong.MaxValue).CheckResult();
        }
        finally
        {
            DeviceApi.vkDestroyFence(fence);
            DeviceApi.vkFreeCommandBuffers(_submitPool, cmd);
        }
    }

    #region Private Methods
    private static List<string> GetOptimalValidationLayers(string[] availableLayers)
    {
        // The preferred validation layer is "VK_LAYER_KHRONOS_validation". Otherwise
        // the LunarG meta layer, then the layers that compose it, then its core layer.
        string[][] candidates =
        [
            ["VK_LAYER_KHRONOS_validation"],
            ["VK_LAYER_LUNARG_standard_validation"],
            [
                "VK_LAYER_GOOGLE_threading",
                "VK_LAYER_LUNARG_parameter_validation",
                "VK_LAYER_LUNARG_object_tracker",
                "VK_LAYER_LUNARG_core_validation",
                "VK_LAYER_GOOGLE_unique_objects",
            ],
            ["VK_LAYER_LUNARG_core_validation"],
        ];

        foreach (var layers in candidates)
        {
            if (layers.All(availableLayers.Contains))
                return [.. layers];
        }

        return [];
    }

    // With a surface, a device must also present to it and have the extension of
    // the swapchain (R-0030, finding V5). With no surface, a graphics queue is enough.
    private static bool IsDeviceSuitable(VkInstanceApi api, VkPhysicalDevice physicalDevice, VkSurfaceKHR surface)
    {
        var checkQueueFamilies = FindQueueFamilies(api, physicalDevice, surface);
        if (checkQueueFamilies.graphicsFamily == VK_QUEUE_FAMILY_IGNORED)
            return false;

        if (surface == VkSurfaceKHR.Null)
            return true;

        if (checkQueueFamilies.presentFamily == VK_QUEUE_FAMILY_IGNORED)
            return false;

        if (!GetDeviceExtensions(api, physicalDevice).Contains(SwapchainExtension))
            return false;

        SwapChainSupportDetails swapChainSupport = Utils.QuerySwapChainSupport(api, physicalDevice, surface);
        return !swapChainSupport.Formats.IsEmpty && !swapChainSupport.PresentModes.IsEmpty;
    }

    [UnmanagedCallersOnly]
    private static uint DebugMessengerCallback(VkDebugUtilsMessageSeverityFlagsEXT messageSeverity,
        VkDebugUtilsMessageTypeFlagsEXT messageTypes,
        VkDebugUtilsMessengerCallbackDataEXT* pCallbackData,
        void* userData)
    {
        Interlocked.Increment(ref s_validationMessages);

        string message = NativeStrings.Read(pCallbackData->pMessage);
        if (messageTypes == VkDebugUtilsMessageTypeFlagsEXT.Validation)
        {
            if (messageSeverity == VkDebugUtilsMessageSeverityFlagsEXT.Error)
            {
                Log.Error($"[Vulkan]: Validation: {messageSeverity} - {message}");
            }
            else if (messageSeverity == VkDebugUtilsMessageSeverityFlagsEXT.Warning)
            {
                Log.Warn($"[Vulkan]: Validation: {messageSeverity} - {message}");
            }

            Debug.WriteLine($"[Vulkan]: Validation: {messageSeverity} - {message}");
        }
        else
        {
            if (messageSeverity == VkDebugUtilsMessageSeverityFlagsEXT.Error)
            {
                Log.Error($"[Vulkan]: {messageSeverity} - {message}");
            }
            else if (messageSeverity == VkDebugUtilsMessageSeverityFlagsEXT.Warning)
            {
                Log.Warn($"[Vulkan]: {messageSeverity} - {message}");
            }

            Debug.WriteLine($"[Vulkan]: {messageSeverity} - {message}");
        }

        return VK_FALSE;
    }

    // The graphics family, and the family that presents to the surface. With no
    // surface, the present family is VK_QUEUE_FAMILY_IGNORED.
    static (uint graphicsFamily, uint presentFamily) FindQueueFamilies(
        VkInstanceApi api, VkPhysicalDevice device, VkSurfaceKHR surface)
    {
        // 3.x returns no span. Ask for the count, then fill an array of that size.
        api.vkGetPhysicalDeviceQueueFamilyProperties(device, out uint queueFamilyCount);
        VkQueueFamilyProperties[] queueFamilies = new VkQueueFamilyProperties[queueFamilyCount];
        api.vkGetPhysicalDeviceQueueFamilyProperties(device, queueFamilies);

        uint graphicsFamily = VK_QUEUE_FAMILY_IGNORED;
        uint presentFamily = VK_QUEUE_FAMILY_IGNORED;
        uint i = 0;
        foreach (VkQueueFamilyProperties queueFamily in queueFamilies)
        {
            if ((queueFamily.queueFlags & VkQueueFlags.Graphics) != VkQueueFlags.None)
            {
                graphicsFamily = i;
            }

            if (surface != VkSurfaceKHR.Null)
            {
                api.vkGetPhysicalDeviceSurfaceSupportKHR(device, i, surface, out VkBool32 presentSupport);
                if (presentSupport)
                {
                    presentFamily = i;
                }
            }

            if (graphicsFamily != VK_QUEUE_FAMILY_IGNORED
                && (surface == VkSurfaceKHR.Null || presentFamily != VK_QUEUE_FAMILY_IGNORED))
            {
                break;
            }

            i++;
        }

        return (graphicsFamily, presentFamily);
    }
    #endregion

    private static readonly Lazy<bool> s_isSupported = new(CheckIsSupported);

    public static bool IsSupported() => s_isSupported.Value;

    private static bool CheckIsSupported()
    {
        try
        {
            VkResult result = vkInitialize();
            if (result != VkResult.Success)
                return false;

            uint propCount;
            result = vkEnumerateInstanceExtensionProperties(&propCount, null);
            if (result != VkResult.Success)
            {
                return false;
            }

            // We require Vulkan 1.1 or higher
            VkVersion version = vkEnumerateInstanceVersion();
            if (version < VkVersion.Version_1_1)
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string[] EnumerateInstanceLayers()
    {
        if (!IsSupported())
        {
            return [];
        }

        uint count = 0;
        VkResult result = vkEnumerateInstanceLayerProperties(&count, null);
        if (result != VkResult.Success || count == 0)
        {
            return [];
        }

        VkLayerProperties[] props = new VkLayerProperties[(int)count];
        vkEnumerateInstanceLayerProperties(props).CheckResult();

        string[] layers = new string[count];
        for (int i = 0; i < count; i++)
        {
            fixed (byte* pLayerName = props[i].layerName)
            {
                layers[i] = NativeStrings.Read(pLayerName);
            }
        }

        return layers;
    }

    // The instance extensions of the implementation, or of one layer.
    private static string[] GetInstanceExtensions(string? layer)
    {
        if (!IsSupported())
        {
            return [];
        }

        using var layerName = new NativeStrings(layer is null ? [] : [layer]);
        byte* pLayerName = layer is null ? null : layerName.First;

        uint count = 0;
        VkResult result = vkEnumerateInstanceExtensionProperties(pLayerName, &count, null);
        if (result != VkResult.Success || count == 0)
        {
            return [];
        }

        VkExtensionProperties* props = stackalloc VkExtensionProperties[(int)count];
        vkEnumerateInstanceExtensionProperties(pLayerName, &count, props).CheckResult();

        string[] extensions = new string[count];
        for (int i = 0; i < count; i++)
        {
            extensions[i] = NativeStrings.Read(props[i].extensionName);
        }

        return extensions;
    }

    private static string[] GetDeviceExtensions(VkInstanceApi api, VkPhysicalDevice physicalDevice)
    {
        uint count = 0;
        api.vkEnumerateDeviceExtensionProperties(physicalDevice, null, &count, null).CheckResult();
        if (count == 0)
            return [];

        var props = new VkExtensionProperties[count];
        string[] extensions = new string[count];
        fixed (VkExtensionProperties* pProps = props)
        {
            api.vkEnumerateDeviceExtensionProperties(physicalDevice, null, &count, pProps).CheckResult();
            for (int i = 0; i < count; i++)
            {
                extensions[i] = NativeStrings.Read(pProps[i].extensionName);
            }
        }

        return extensions;
    }
}
