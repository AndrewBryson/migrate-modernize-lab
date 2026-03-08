package com.microsoft.migration.assets.worker.config;

import com.azure.spring.messaging.ConsumerIdentifier;
import com.azure.spring.messaging.PropertiesSupplier;
import com.azure.spring.messaging.servicebus.core.properties.ProcessorProperties;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

@Configuration
public class RabbitConfig {
    public static final String IMAGE_PROCESSING_QUEUE = "image-processing";

    @Bean
    public PropertiesSupplier<ConsumerIdentifier, ProcessorProperties> processorPropertiesSupplier() {
        return key -> {
            ProcessorProperties processorProperties = new ProcessorProperties();
            // Set autoComplete to false to match RabbitMQ's AcknowledgeMode.MANUAL behavior
            processorProperties.setAutoComplete(false);
            return processorProperties;
        };
    }
}
