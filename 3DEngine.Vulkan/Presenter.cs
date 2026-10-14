// Copyright (c) Amer Koleci and Contributors.
// Licensed under the MIT License (MIT). See THIRD-PARTY-NOTICES.md, section 4.
// This file comes from the Vortice.Vulkan sample framework. TASK-0027 moved it
// into this project and changed it. ADR-0017 gives the rules for this project.

using Vortice.Vulkan;

namespace ThreeDEngine.Vulkan;

// The presentation to a window (ADR-0025 item 2): the swapchain, the fence, the
// command pool and the semaphores of each frame, the acquire of an image and its
// present. TASK-0076 moved it out of GraphicsDevice, so that a device can exist with
// no window. The device creates it and disposes it.
public sealed unsafe class Presenter : IDisposable
{
    private readonly GraphicsDevice _device;
    private readonly VkQueue _presentQueue;
    private readonly PerFrame[] _perFrame;
    private readonly List<VkSemaphore> _recycledSemaphores = new();
    private uint _frameIndex;

    internal Presenter(GraphicsDevice device, VkSurfaceKHR surface, Window window, VkQueue presentQueue)
    {
        _device = device;
        _presentQueue = presentQueue;

        Swapchain = new Swapchain(device, surface, window);
        _perFrame = new PerFrame[Swapchain.ImageCount];
        for (int i = 0; i < _perFrame.Length; i++)
        {
            device.DeviceApi.vkCreateFence(VkFenceCreateFlags.Signaled, out _perFrame[i].QueueSubmitFence).CheckResult();

            VkCommandPoolCreateInfo poolCreateInfo = new()
            {
                flags = VkCommandPoolCreateFlags.Transient,
                queueFamilyIndex = device.GraphicsFamily,
            };
            device.DeviceApi.vkCreateCommandPool(&poolCreateInfo, null, out _perFrame[i].PrimaryCommandPool).CheckResult();

            device.DeviceApi.vkAllocateCommandBuffer(_perFrame[i].PrimaryCommandPool, out _perFrame[i].PrimaryCommandBuffer).CheckResult();
        }
    }

    public Swapchain Swapchain { get; }

    // The render pass of the swapchain, for the pipeline of the window.
    public VkRenderPass RenderPass => Swapchain.RenderPass;

    // The caller waits until the device is idle.
    public void Dispose()
    {
        var api = _device.DeviceApi;
        Swapchain.Dispose();

        for (var i = 0; i < _perFrame.Length; i++)
        {
            api.vkDestroyFence(_perFrame[i].QueueSubmitFence);

            if (_perFrame[i].PrimaryCommandBuffer != IntPtr.Zero)
            {
                api.vkFreeCommandBuffers(_perFrame[i].PrimaryCommandPool, _perFrame[i].PrimaryCommandBuffer);

                _perFrame[i].PrimaryCommandBuffer = IntPtr.Zero;
            }

            api.vkDestroyCommandPool(_perFrame[i].PrimaryCommandPool);

            if (_perFrame[i].SwapchainAcquireSemaphore != VkSemaphore.Null)
            {
                api.vkDestroySemaphore(_perFrame[i].SwapchainAcquireSemaphore);
                _perFrame[i].SwapchainAcquireSemaphore = VkSemaphore.Null;
            }

            if (_perFrame[i].SwapchainReleaseSemaphore != VkSemaphore.Null)
            {
                api.vkDestroySemaphore(_perFrame[i].SwapchainReleaseSemaphore);
                _perFrame[i].SwapchainReleaseSemaphore = VkSemaphore.Null;
            }
        }

        foreach (VkSemaphore semaphore in _recycledSemaphores)
        {
            api.vkDestroySemaphore(semaphore);
        }
        _recycledSemaphores.Clear();
    }

    public void RenderFrame(Action<VkCommandBuffer, VkFramebuffer, VkExtent2D> draw)
    {
        var api = _device.DeviceApi;
        VkResult result = AcquireNextImage(out _frameIndex);

        // Handle outdated error in acquire.
        if (result == VkResult.SuboptimalKHR || result == VkResult.ErrorOutOfDateKHR)
        {
            //Resize(context.swapchain_dimensions.width, context.swapchain_dimensions.height);
            result = AcquireNextImage(out _frameIndex);
        }

        if (result != VkResult.Success)
        {
            api.vkDeviceWaitIdle();
            return;
        }

        // Begin command recording
        VkCommandBuffer cmd = _perFrame[_frameIndex].PrimaryCommandBuffer;

        VkCommandBufferBeginInfo beginInfo = new()
        {
            flags = VkCommandBufferUsageFlags.OneTimeSubmit
        };
        api.vkBeginCommandBuffer(cmd, &beginInfo).CheckResult();

        draw(cmd, Swapchain.Framebuffers[_frameIndex], Swapchain.Extent);

        // Complete the command buffer.
        api.vkEndCommandBuffer(cmd).CheckResult();

        if (_perFrame[_frameIndex].SwapchainReleaseSemaphore == VkSemaphore.Null)
        {
            api.vkCreateSemaphore(out _perFrame[_frameIndex].SwapchainReleaseSemaphore).CheckResult();
        }

        VkPipelineStageFlags wait_stage = VkPipelineStageFlags.ColorAttachmentOutput;
        VkSemaphore waitSemaphore = _perFrame[_frameIndex].SwapchainAcquireSemaphore;
        VkSemaphore signalSemaphore = _perFrame[_frameIndex].SwapchainReleaseSemaphore;

        VkSubmitInfo submitInfo = new()
        {
            commandBufferCount = 1u,
            pCommandBuffers = &cmd,
            waitSemaphoreCount = 1u,
            pWaitSemaphores = &waitSemaphore,
            pWaitDstStageMask = &wait_stage,
            signalSemaphoreCount = 1u,
            pSignalSemaphores = &signalSemaphore
        };

        // Submit command buffer to graphics queue
        api.vkQueueSubmit(_device.GraphicsQueue, submitInfo, _perFrame[_frameIndex].QueueSubmitFence);

        result = PresentImage(_frameIndex);

        // Handle Outdated error in present.
        if (result == VkResult.SuboptimalKHR || result == VkResult.ErrorOutOfDateKHR)
        {
            //Resize(context.swapchain_dimensions.width, context.swapchain_dimensions.height);
        }
        else if (result != VkResult.Success)
        {
            Log.Error("Failed to present swapchain image.");
        }
    }

    private VkResult AcquireNextImage(out uint imageIndex)
    {
        var api = _device.DeviceApi;
        VkSemaphore acquireSemaphore;
        if (_recycledSemaphores.Count == 0)
        {
            api.vkCreateSemaphore(out acquireSemaphore).CheckResult();
        }
        else
        {
            acquireSemaphore = _recycledSemaphores[_recycledSemaphores.Count - 1];
            _recycledSemaphores.RemoveAt(_recycledSemaphores.Count - 1);
        }

        VkResult result = api.vkAcquireNextImageKHR(Swapchain.Handle, ulong.MaxValue, acquireSemaphore, VkFence.Null, out imageIndex);

        if (result != VkResult.Success)
        {
            _recycledSemaphores.Add(acquireSemaphore);
            return result;
        }

        if (_perFrame[imageIndex].QueueSubmitFence != VkFence.Null)
        {
            api.vkWaitForFences(_perFrame[imageIndex].QueueSubmitFence, true, ulong.MaxValue);
            api.vkResetFences(_perFrame[imageIndex].QueueSubmitFence);
        }

        if (_perFrame[imageIndex].PrimaryCommandPool != VkCommandPool.Null)
        {
            api.vkResetCommandPool(_perFrame[imageIndex].PrimaryCommandPool, VkCommandPoolResetFlags.None);
        }

        // Recycle the old semaphore back into the semaphore manager.
        VkSemaphore oldSemaphore = _perFrame[imageIndex].SwapchainAcquireSemaphore;

        if (oldSemaphore != VkSemaphore.Null)
        {
            _recycledSemaphores.Add(oldSemaphore);
        }

        _perFrame[imageIndex].SwapchainAcquireSemaphore = acquireSemaphore;

        return VkResult.Success;
    }

    private VkResult PresentImage(uint imageIndex)
    {
        return _device.DeviceApi.vkQueuePresentKHR(_presentQueue, _perFrame[imageIndex].SwapchainReleaseSemaphore, Swapchain.Handle, imageIndex);
    }

    private struct PerFrame
    {
        public VkFence QueueSubmitFence;
        public VkCommandPool PrimaryCommandPool;
        public VkCommandBuffer PrimaryCommandBuffer;
        public VkSemaphore SwapchainAcquireSemaphore;
        public VkSemaphore SwapchainReleaseSemaphore;
    }
}
